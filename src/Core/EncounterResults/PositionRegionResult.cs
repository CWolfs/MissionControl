using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using System.Linq;
using System.Collections.Generic;

using MissionControl.Utils;

/**
	This result will reposition a region within a min and max threshold. 
	It will also recreate the Mesh to match the terrain for triggering the region correctly
*/
namespace MissionControl.Result {
	public class PositionRegionResult : EncounterResult {
		public string RegionName { get; set; } = "Region_Escape";
		private static float REGION_RADIUS = 70.71068f;

		public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
			Main.LogDebug("[PositionRegion] Positioning Region...");
			GameObject regionGo = GameObject.Find(RegionName);
			CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
			Team playerTeam = combatState.LocalPlayerTeam;

			Vector3 centerOfTeamMass = GetCenterOfTeamMass(playerTeam);
			Vector3 possiblePosition = Vector3.zero;
			AbstractActor actor = combatState.AllActors.First((AbstractActor x) => x.TeamId == playerTeam.GUID);

			while (possiblePosition == Vector3.zero || !PathFinderManager.Instance.IsSpawnValid(possiblePosition, actor.GameRep.transform.position, UnitType.Mech)) {
				Main.LogDebug($"[PositionRegion] {(possiblePosition == Vector3.zero ? "Finding possible position..." : "Trying again to find a possible position...")}");
				possiblePosition = SceneUtils.GetRandomPositionFromTarget(centerOfTeamMass, Main.Settings.DynamicWithdraw.MinDistanceForZone, Main.Settings.DynamicWithdraw.MaxDistanceForZone);
			}

			regionGo.transform.position = possiblePosition;

			// Debug
			// GameObjextExtensions.CreateDebugPoint("DEBUGCenterofTeamMassGizmo", centerOfTeamMass, Color.red);
			// GameObjextExtensions.CreateDebugPoint("DEBUGDynamicWithdrawCenter", regionGo.transform.position, Color.blue);

			RegenerateRegion(regionGo);
		}

		private Vector3 GetCenterOfTeamMass(Team team) {
			CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
			List<AbstractActor> teamActors = combatState.AllActors.FindAll((AbstractActor x) => x.TeamId == team.GUID);
			return SceneUtils.CalculateCentroidOfActors(teamActors);
		}

		private void RegenerateRegion(GameObject regionGo) {
			CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
			RegionGameLogic regionGameLogic = regionGo.GetComponent<RegionGameLogic>();
			List<Vector3> meshPoints = new List<Vector3>();

			// Get all region points and fix the y height
			foreach (Transform t in regionGo.transform) {
				if (t.gameObject.name.StartsWith("RegionPoint")) {
					Vector3 position = t.position;
					float height = combatState.MapMetaData.GetLerpedHeightAt(position);
					Vector3 fixedHeightPosition = new Vector3(position.x, height, position.z);
					t.position = fixedHeightPosition;
					meshPoints.Add(t.localPosition);
				}
			}

			// Create new mesh from points and set to collisder and mesh filter
			MeshCollider collider = regionGo.GetComponent<MeshCollider>();
			MeshFilter mf = regionGo.GetComponent<MeshFilter>();
			Mesh mesh = MeshTools.CreateHexigon(REGION_RADIUS, meshPoints);
			collider.sharedMesh = mesh;
			mf.mesh = mesh;

			List<MapEncounterLayerDataCell> cells = SceneUtils.GetMapEncounterLayerDataCellsWithinCollider(regionGo);
			for (int i = 0; i < cells.Count; i++) {
				MapEncounterLayerDataCell cell = cells[i];
				cell.AddRegion(regionGameLogic);
			}
		}
	}
}
