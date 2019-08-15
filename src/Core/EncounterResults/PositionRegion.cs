using UnityEngine;

using BattleTech;

using System.Reflection;
using System.Collections.Generic;

using MissionControl.Utils;

using Harmony;

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

			// TODO: Experiment: I think that I need to iterate over the hex positions in the cell data and add the region to each position
			// This is then checked when a unit enters any location - it checks this for the region ID and sends a broadcast
 			// MapMetaDataExporter mapMetaDataExporter = MissionControl.Instance.EncounterLayerParentGameObject.GetComponent<MapMetaDataExporter>();
			// EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
			// MapMetaData mapMetaData = combat.MapMetaData;
			// Vector3 regionPosition = regionGo.transform.position;
			// int cellX = encounterLayerData.GetXIndex(regionPosition.x);
			// int cellZ = encounterLayerData.GetZIndex(regionPosition.z);

			/*
			MapEncounterLayerDataCell encounterLayerDataCell = encounterLayerData.GetCellAt(cellX, cellZ);
			if (encounterLayerDataCell == null) encounterLayerDataCell = new MapEncounterLayerDataCell();
			encounterLayerDataCell.AddRegion(regionGo.GetComponent<RegionGameLogic>());
			encounterLayerData.mapEncounterLayerDataCells[cellX, cellZ] = encounterLayerDataCell;
			*/
			List<MapEncounterLayerDataCell> cells = SceneUtils.GetMapEncounterLayerDataCellsWithinCollider(regionGo);
			for (int i = 0; i < cells.Count; i++) {
				MapEncounterLayerDataCell cell = cells[i];
				cell.AddRegion(regionGameLogic);
			}
		}
	}
}
