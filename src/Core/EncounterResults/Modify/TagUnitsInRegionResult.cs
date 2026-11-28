using BattleTech;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MissionControl.Result {
  public class TagUnitsInRegionResult : EncounterResult {
    public string RegionGuid { get; set; }
    public int NumberOfUnits { get; set; } = 0;
    public string Type { get; set; }
    public string[] Tags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      string[] validTags = Tags?.Select(t => t?.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToArray() ?? Array.Empty<string>();
      if (validTags.Length == 0) {
        Main.Logger.LogError($"[TagUnitsInRegionResult] No valid tags provided for region '{RegionGuid}'. Skipping.");
        return;
      }

      if (NumberOfUnits < 0) {
        Main.Logger.LogError($"[TagUnitsInRegionResult] Invalid negative NumberOfUnits '{NumberOfUnits}' for region '{RegionGuid}'. Skipping.");
        return;
      }

      Main.LogDebug($"[TagUnitsInRegionResult] Tagging '{NumberOfUnits}' '{Type}' in region '{RegionGuid}' with tags '{string.Join(",", validTags)}'");
      TagUnitsInRegion(validTags);
    }

    private void TagUnitsInRegion(string[] validTags) {
      CombatGameState combat = this.combat ?? UnityGameInstance.BattleTechGame?.Combat;
      if (combat == null) {
        Main.Logger.LogError("[TagUnitsInRegionResult] Combat state is null.");
        return;
      }

      RegionGameLogic regionGameLogic = combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[TagUnitsInRegionResult] Region Not Found for Guid '{RegionGuid}'");
        return;
      }

      EncounterLayerData layerData = combat.EncounterLayerData ?? MissionControl.Instance.EncounterLayerData;

      if (string.Equals(Type, "Building", StringComparison.OrdinalIgnoreCase)) {
        List<BattleTech.Building> buildingsInMap = combat.ItemRegistry.GetObjectsOfType<BattleTech.Building>(TaggedObjectType.Building);
        Main.LogDebug($"[TagUnitsInRegionResult] Collected '{buildingsInMap.Count}' buildings to check.");

        if (NumberOfUnits > 0) {
          buildingsInMap.Shuffle();
        }

        int processedUnitCount = 0;
        foreach (BattleTech.Building building in buildingsInMap) {
          if (building == null || building.IsDead) continue;

          BuildingRepresentation buildingRep = building.BuildingRep;
          if (buildingRep == null || !buildingRep.gameObject.activeInHierarchy) continue;

          bool isBuildingInRegion = layerData != null && layerData.IsPointInRegion(buildingRep.transform.position, RegionGuid);
          if (isBuildingInRegion) {
            Main.LogDebug($"[TagUnitsInRegionResult] Found building '{buildingRep.gameObject.name}' in region!");
            building.EncounterTags.UnionWith(validTags);

            processedUnitCount += 1;
            if (NumberOfUnits > 0 && processedUnitCount >= NumberOfUnits) break;
          }
        }
      } else if (IsActorUnitType(Type)) {
        List<AbstractActor> actorsInMap = new List<AbstractActor>(combat.AllActors);
        Main.LogDebug($"[TagUnitsInRegionResult] Collected '{actorsInMap.Count}' actors to check for type '{Type}'.");

        if (NumberOfUnits > 0) {
          actorsInMap.Shuffle();
        }

        int processedUnitCount = 0;
        foreach (AbstractActor actor in actorsInMap) {
          if (actor == null || actor.IsDead) continue;
          if (!MatchesActorUnitType(actor, Type)) continue;

          bool isActorInRegion = layerData != null
            ? layerData.IsPointInRegion(actor.CurrentPosition, RegionGuid)
            : actor.IsInRegion(RegionGuid);
          if (isActorInRegion) {
            Main.LogDebug($"[TagUnitsInRegionResult] Found actor '{actor.DisplayName}' ({actor.UnitType}) in region!");
            actor.EncounterTags.UnionWith(validTags);

            processedUnitCount += 1;
            if (NumberOfUnits > 0 && processedUnitCount >= NumberOfUnits) break;
          }
        }
      } else {
        Main.Logger.LogWarning($"[TagUnitsInRegionResult] Tagging '{Type}' is not supported.");
      }
    }

    private static bool IsActorUnitType(string unitType) {
      return string.Equals(unitType, "Unit", StringComparison.OrdinalIgnoreCase)
        || string.Equals(unitType, "Any", StringComparison.OrdinalIgnoreCase)
        || string.Equals(unitType, "Actor", StringComparison.OrdinalIgnoreCase)
        || string.Equals(unitType, "Mech", StringComparison.OrdinalIgnoreCase)
        || string.Equals(unitType, "Vehicle", StringComparison.OrdinalIgnoreCase)
        || string.Equals(unitType, "Turret", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesActorUnitType(AbstractActor actor, string unitType) {
      if (string.Equals(unitType, "Unit", StringComparison.OrdinalIgnoreCase)
        || string.Equals(unitType, "Any", StringComparison.OrdinalIgnoreCase)
        || string.Equals(unitType, "Actor", StringComparison.OrdinalIgnoreCase)) {
        return true;
      }

      if (string.Equals(unitType, "Mech", StringComparison.OrdinalIgnoreCase)) return actor.UnitType == UnitType.Mech;
      if (string.Equals(unitType, "Vehicle", StringComparison.OrdinalIgnoreCase)) return actor.UnitType == UnitType.Vehicle;
      if (string.Equals(unitType, "Turret", StringComparison.OrdinalIgnoreCase)) return actor.UnitType == UnitType.Turret;
      return false;
    }
  }
}
