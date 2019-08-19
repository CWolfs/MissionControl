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
	public class PositionRegion : EncounterResult {
		public string RegionName { get; set; } = "Region_Escape";
		private static float REGION_RADIUS = 70.71068f;

		public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
			Main.LogDebug("[PositionRegion] Positioning Region...");
			GameObject regionGo = GameObject.Find(RegionName);
			CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
			Team playerTeam = combatState.LocalPlayerTeam;

			Vector3 centerOfTeamMass = GetCenterOfTeamMass(playerTeam);

			regionGo.transform.position = SceneUtils.GetRandomPositionFromTarget(centerOfTeamMass, Main.Settings.DynamicWithdraw.MinDistanceForZone, Main.Settings.DynamicWithdraw.MaxDistanceForZone);

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

			ContractObjectiveGameLogic[] existingContractObjectives = MissionControl.Instance.EncounterLayerData.GetComponents<ContractObjectiveGameLogic>();
			existingContractObjectives.ToList().ForEach(obj => {
				obj.OverrideRequiredByMission(false);
				obj.objectiveRefList.ForEach(objective => {
					Main.LogDebug("[PositionRegion] Objective name is: " + objective.encounterObject.name);
					if (objective.encounterObject.name != "Objective_Escape") {
						objective.encounterObject.CompleteObjective("Dynamic Withdraw Triggered", CompleteObjectiveType.Failed, true, true);
					}
				});
				ReflectionHelper.SetPrivateField(obj, "currentObjectiveStatus", ObjectiveStatus.Failed);
			});
			
			// debug - this doesn't work for some reason. Need to figure out later why.
			// regionGameLogic.MarkAssociatedCellsAsDangerous(true);
		}
	}
}
