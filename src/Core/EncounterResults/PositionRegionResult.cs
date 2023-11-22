using UnityEngine;

using BattleTech;

using System.Linq;
using System.Collections.Generic;

/**
	This result will reposition a region within a min and max threshold. 
	It will also recreate the Mesh to match the terrain for triggering the region correctly
*/
namespace MissionControl.Result {
  public class PositionRegionResult : EncounterResult {
    public string RegionName { get; set; } = "";

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[PositionRegion] Positioning Region...");
      GameObject regionGo = GameObject.Find(RegionName);
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Team playerTeam = combatState.LocalPlayerTeam;

      Vector3 centerOfTeamMass = GetCenterOfTeamMass(playerTeam, true);
      Vector3 possiblePosition = Vector3.zero;
      AbstractActor actor = combatState.AllActors.First((AbstractActor x) => x.TeamId == playerTeam.GUID);

      while (possiblePosition == Vector3.zero || !PathFinderManager.Instance.IsSpawnValid(regionGo, possiblePosition, actor.GameRep.transform.position, UnitType.Mech, $"PositionRegionResult.{RegionName}")) {
        Main.LogDebug($"[PositionRegion] {(possiblePosition == Vector3.zero ? "Finding possible position..." : "Trying again to find a possible position...")}");
        possiblePosition = SceneUtils.GetRandomPositionFromTarget(centerOfTeamMass, Main.Settings.DynamicWithdraw.MinDistanceForZone, Main.Settings.DynamicWithdraw.MaxDistanceForZone);
      }
      regionGo.transform.position = possiblePosition;

      // Debug
      // GameObjextExtensions.CreateDebugPoint("DEBUGCenterofTeamMassGizmo", centerOfTeamMass, Color.red);
      // GameObjextExtensions.CreateDebugPoint("DEBUGDynamicWithdrawCenter", regionGo.transform.position, Color.blue);

      RegionGameLogic regionGameLogic = regionGo.GetComponent<RegionGameLogic>();
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
