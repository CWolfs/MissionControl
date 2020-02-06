using UnityEngine;

using BattleTech;

using System.Collections.Generic;

using MissionControl.Utils;

public static class RegionGameLogicExtensions {
  public static void Regenerate(this RegionGameLogic regionGameLogic) {
    GameObject regionGo = regionGameLogic.gameObject;
    CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
    List<Vector3> meshPoints = new List<Vector3>();

    // Remove old region location from layer data cells
    // TODO: Uncomment when I've fixed regions to work again
    List<MapEncounterLayerDataCell> beforeCells = SceneUtils.GetMapEncounterLayerDataCellsWithinCollider(regionGo);
    for (int i = 0; i < beforeCells.Count; i++) {
      MapEncounterLayerDataCell cell = beforeCells[i];
      cell.RemoveRegion(regionGameLogic);
    }

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

    // Create new mesh from points and set to collider and mesh filter
    MeshCollider collider = regionGo.GetComponent<MeshCollider>();
    MeshFilter mf = regionGo.GetComponent<MeshFilter>();
    Mesh mesh = MeshTools.CreateHexigon(regionGameLogic.radius, meshPoints);
    collider.sharedMesh = mesh;
    mf.mesh = mesh;

    List<MapEncounterLayerDataCell> afterCells = SceneUtils.GetMapEncounterLayerDataCellsWithinCollider(regionGo);
    for (int i = 0; i < afterCells.Count; i++) {
      MapEncounterLayerDataCell cell = afterCells[i];
      cell.AddRegion(regionGameLogic);
    }
  }
}