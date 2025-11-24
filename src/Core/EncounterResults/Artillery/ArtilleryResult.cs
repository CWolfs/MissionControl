using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Result {
  public class ArtilleryResult : EncounterResult {
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> RegionIDs { get; set; }
    public bool ShotsFireAtAllRegions { get; set; } = true;
    public int Damage { get; set; }
    public int HeatDamage { get; set; }
    public int StabilityDamage { get; set; }
    public ArtilleryVFXType ArtilleryVFXType { get; set; } = ArtilleryVFXType.ArtilleryShellBarrage;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[ArtilleryResult] Triggering artillery strike '{Name}' - {Description}");

      if (RegionIDs == null || RegionIDs.Count == 0) {
        Main.Logger.LogError($"[ArtilleryResult] No regions specified for artillery strike '{Name}'");
        return;
      }

      List<string> regionsToStrike = new List<string>();

      if (ShotsFireAtAllRegions) {
        // Fire one shot at each region
        regionsToStrike.AddRange(RegionIDs);
        Main.LogDebug($"[ArtilleryResult] Firing artillery at all {RegionIDs.Count} regions");
      } else {
        // Pick a random region
        int randomIndex = UnityEngine.Random.Range(0, RegionIDs.Count);
        regionsToStrike.Add(RegionIDs[randomIndex]);
        Main.LogDebug($"[ArtilleryResult] Firing artillery at 1 random region (index {randomIndex})");
      }

      LaunchArtillery(regionsToStrike);
    }

    private void LaunchArtillery(List<string> regionGuids) {
      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;
      List<ICombatant> allTargets = new List<ICombatant>();
      List<Vector3> targetPositions = new List<Vector3>();

      foreach (string regionGuid in regionGuids) {
        RegionGameLogic region = combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(regionGuid);

        if (region == null) {
          Main.Logger.LogError($"[ArtilleryResult] Could not find region with GUID '{regionGuid}'");
          continue;
        }

        // Get the region position
        Vector3 position = region.Position;
        position.y = region.GetAverageTerrainHeightForOccupiedCells();
        targetPositions.Add(position);

        Main.LogDebug($"[ArtilleryResult] Checking region '{regionGuid}' for targets");

        // Find all actors in the region
        foreach (AbstractActor actor in combat.AllActors) {
          if (actor.IsInRegion(regionGuid)) {
            allTargets.Add(actor);
            Main.LogDebug($"[ArtilleryResult] Found actor '{actor.DisplayName}' in region");
          }
        }

        // Find all buildings in the region
        foreach (string buildingGuid in region.buildingGuidsInRegion) {
          ObstructionGameLogic obstruction = combat.ItemRegistry.GetItemByGUID<ObstructionGameLogic>(buildingGuid);

          if (obstruction == null) {
            Main.Logger.LogError($"[ArtilleryResult] Couldn't find obstruction: [{buildingGuid}]");
            continue;
          }

          if (obstruction.enabled) {
            BattleTech.Building building = combat.ItemRegistry.GetItemByGUID<BattleTech.Building>(ObstructionGameLogic.GetBuildingRepGuid(buildingGuid));

            if (building == null) {
              Main.LogDebug($"[ArtilleryResult] No building rep for obstruction with guid [{buildingGuid}]");
            } else {
              allTargets.Add(building);
              Main.LogDebug($"[ArtilleryResult] Found building in region");
            }
          }
        }
      }

      Main.LogDebug($"[ArtilleryResult] Artillery strike on {targetPositions.Count} positions with {allTargets.Count} targets");
      Main.LogDebug($"[ArtilleryResult] Damage: {Damage}, Heat: {HeatDamage}, Stability: {StabilityDamage}, VFX: {ArtilleryVFXType}");

      // Create and enqueue the artillery sequence
      ScalableArtilleryObjectiveSequence artillerySequence = new ScalableArtilleryObjectiveSequence(
        combat,
        targetPositions,
        ArtilleryVFXType,
        allTargets,
        Damage,
        HeatDamage,
        StabilityDamage,
        TerrainMaskFlags.None,
        vfxScale: 1.0f
      );

      EncounterLayerParent.EnqueueLoadAwareMessage(new AddSequenceToStackMessage(artillerySequence));
      Main.LogDebug($"[ArtilleryResult] Artillery sequence enqueued");
    }
  }
}
