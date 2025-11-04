using UnityEngine;

using BattleTech;

using System.Collections.Generic;

/**
	This result will reposition a region within a min and max threshold. 
	It will also recreate the Mesh to match the terrain for triggering the region correctly
*/
namespace MissionControl.Result {
  public class PositionRegionResult : EncounterResult {
    // TODO: Replace this ideally with GUID and not name
    public string RegionName { get; set; } = "";

    private const int MAX_ATTEMPTS = 100;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[PositionRegion] Positioning Region...");
      GameObject regionGo = GameObject.Find(RegionName);

      if (regionGo == null) {
        Main.Logger.LogError($"[PositionRegion] Cannot find region GameObject with name '{RegionName}'. Aborting region positioning.");
        return;
      }

      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Team playerTeam = combatState.LocalPlayerTeam;

      Vector3 centerOfTeamMass = GetCenterOfTeamMass(playerTeam, true);
      Vector3 possiblePosition = Vector3.zero;
      AbstractActor actor = combatState.AllActors.Find(x => x.TeamId == playerTeam.GUID);

      if (actor == null) {
        Main.Logger.LogError($"[PositionRegion] Cannot find any actors for player team '{playerTeam.GUID}'. Aborting region positioning.");
        return;
      }

      int attempts = 0;

      while (possiblePosition == Vector3.zero || !PathFinderManager.Instance.IsSpawnValid(regionGo, possiblePosition, actor.GameRep.transform.position, UnitType.Mech, $"PositionRegionResult.{RegionName}")) {
        Main.LogDebug($"[PositionRegion] {(possiblePosition == Vector3.zero ? "Finding possible position..." : "Trying again to find a possible position...")}");
        possiblePosition = SceneUtils.GetRandomPositionFromTarget(centerOfTeamMass, Main.Settings.DynamicWithdraw.MinDistanceForZone, Main.Settings.DynamicWithdraw.MaxDistanceForZone);

        attempts++;
        if (attempts >= MAX_ATTEMPTS) {
          Main.Logger.LogError($"[PositionRegion] Failed to find valid position for region '{RegionName}' after {MAX_ATTEMPTS} attempts. Aborting region positioning.");
          return;
        }
      }

      regionGo.transform.position = possiblePosition;

      // Debug
      // GameObjextExtensions.CreateDebugPoint("DEBUGCenterofTeamMassGizmo", centerOfTeamMass, Color.red);
      // GameObjextExtensions.CreateDebugPoint("DEBUGDynamicWithdrawCenter", regionGo.transform.position, Color.blue);

      RegionGameLogic regionGameLogic = regionGo.GetComponent<RegionGameLogic>();

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[PositionRegion] Region GameObject '{RegionName}' does not have a RegionGameLogic component. Cannot regenerate.");
        return;
      }

      regionGameLogic.Regenerate();
    }

    private Vector3 GetCenterOfTeamMass(Team team, bool avoidEnemies) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      List<AbstractActor> teamActors = combatState.AllActors.FindAll((AbstractActor x) => x.TeamId == team.GUID);
      List<AbstractActor> avoidTeamActors = null;

      if (avoidEnemies) avoidTeamActors = combatState.AllEnemies;

      return SceneUtils.CalculateCentroidOfActors(teamActors, avoidTeamActors);
    }
  }
}
