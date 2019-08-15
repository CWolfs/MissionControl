using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl;

public static class SceneUtils {
    public static Vector3 GetRandomPositionWithinBounds(Vector3 target, float maxDistance) {
      GameObject chunkBoundaryRect = MissionControl.MissionControl.Instance.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();

      Vector3 randomRecPosition = boundaryRec.GetRandomPositionFromTarget(target, maxDistance);
      return randomRecPosition.GetClosestHexLerpedPointOnGrid();
    }

    public static Vector3 GetRandomPositionFromTarget(Vector3 targPosition, float minDistance, float maxDistance) {
      Vector3 targetPosition = targPosition.GetClosestHexLerpedPointOnGrid();
      Vector3 randomPositionWithinBounds = GetRandomPositionWithinBounds(targPosition, maxDistance);

      if (!IsWithinBoundedDistanceOfTarget(targetPosition, randomPositionWithinBounds, minDistance, maxDistance)) {
        return GetRandomPositionFromTarget(targetPosition, minDistance, maxDistance);
      } else {
        return randomPositionWithinBounds;
      }
    }

    public static bool IsWithinBoundedDistanceOfTarget(Vector3 origin, Vector3 target, float minDistance, float maxDistance) {
      Vector3 vectorToTarget = target - origin;
      vectorToTarget.y = 0;
      float distance = vectorToTarget.magnitude;
      if ((distance > minDistance) && (distance < maxDistance)) return true;
      MissionControl.Main.LogDebugWarning($"[IsWithinBoundedDistanceOfTarget] Distance is {distance} and so not within bounds. Getting new random position");
      return false;
    }
    
    public static Vector3 CalculateCentroidOfActors(List<AbstractActor> actors) {
      float totalMass = 1;
      Vector3 centroid = Vector3.zero;

      for (int i = 0; i < actors.Count; i++) {
        AbstractActor actor = actors[i];
        Vector3 position = actor.GameRep.transform.position;
        float weight = 1; // placeholder weighting
        
        centroid += position * totalMass;
        totalMass += weight;
      }

      centroid /= totalMass;
      
      return centroid;
    }

    public static List<MapEncounterLayerDataCell> GetMapEncounterLayerDataCellsWithinCollider(GameObject regionGo) {
      MeshCollider collider = regionGo.GetComponent<MeshCollider>();
      RegionGameLogic regionGameLogic = regionGo.GetComponent<RegionGameLogic>();
      List<MapEncounterLayerDataCell> cells = new List<MapEncounterLayerDataCell>();
      Vector3 colliderExtents = collider.bounds.extents;
      Vector3 colliderCenter = collider.bounds.center;

      EncounterLayerData encounterLayerData = MissionControl.MissionControl.Instance.EncounterLayerData;
      int cellX = encounterLayerData.GetXIndex(colliderCenter.x);
      int cellZ = encounterLayerData.GetZIndex(colliderCenter.z);
      MapEncounterLayerDataCell layerDataCell = GetOrCreateEncounterLayerDataCell(cellX, cellZ);
      
      cells.Add(layerDataCell);

      return cells;
    }

    public static MapEncounterLayerDataCell GetOrCreateEncounterLayerDataCell(int x, int y) {
      MapEncounterLayerDataCell encounterLayerDataCell = MissionControl.MissionControl.Instance.EncounterLayerData.GetCellAt(x, y);
      if (encounterLayerDataCell == null) encounterLayerDataCell = new MapEncounterLayerDataCell();
      return encounterLayerDataCell;
    }
}