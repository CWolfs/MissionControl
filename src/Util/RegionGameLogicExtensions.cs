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

    // Reajust the height to factor in -30f fudge rotation. This works but I need to see if I can remove this and fix it properly
    Vector3[] vertices = mesh.vertices;

    for (int i = 0; i < vertices.Length; i++) {
      Vector3 worldVertexPos = regionGo.transform.TransformPoint(vertices[i]);       // Convert local vertex position to world position
      float height = combatState.MapMetaData.GetLerpedHeightAt(worldVertexPos);
      worldVertexPos.y = height;
      vertices[i] = regionGo.transform.InverseTransformPoint(worldVertexPos); // Convert the adjusted world position back to local position
    }

    // Apply the changes to the mesh
    mesh.vertices = vertices;
    mesh.RecalculateBounds();

    List<MapEncounterLayerDataCell> afterCells = SceneUtils.GetMapEncounterLayerDataCellsWithinCollider(regionGo);
    for (int i = 0; i < afterCells.Count; i++) {
      MapEncounterLayerDataCell cell = afterCells[i];
      cell.AddRegion(regionGameLogic);
    }
  }
}