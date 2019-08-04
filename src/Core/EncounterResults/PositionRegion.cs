using UnityEngine;

using BattleTech;

using System.Collections.Generic;

/**
	This result will reposition a region within a min and max threshold. 
	It will also recreate the Mesh to match the terrain for triggering the region correctly
*/
namespace MissionControl.Result {
	public class PositionRegion : EncounterResult {
		public string RegionName { get; set; } = "Region_Escape";

		public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
			Main.LogDebug("[PositionRegion] Positioning Region...");
			GameObject regionGo = GameObject.Find(RegionName);
			CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
			Team playerTeam = combatState.LocalPlayerTeam;

			Vector3 centerOfTeamMass = GetCenterOfTeamMass(playerTeam);

			regionGo.transform.position = SceneUtils.GetRandomPositionFromTarget(centerOfTeamMass, Main.Settings.DynamicWithdraw.MinDistanceForZone, Main.Settings.DynamicWithdraw.MaxDistanceForZone);
		}

		private Vector3 GetCenterOfTeamMass(Team team) {
			CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
			List<AbstractActor> teamActors = combatState.AllActors.FindAll((AbstractActor x) => x.TeamId == team.GUID);
			return SceneUtils.CalculateCentroidOfActors(teamActors);
		}
	}
}
