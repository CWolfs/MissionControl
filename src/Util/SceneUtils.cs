using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

public static class SceneUtils {
  public static Vector3 GetRandomPositionWithinBounds(Vector3 target, float maxDistance) {
    EncounterBoundaryChunkGameLogic chunkBoundary = MissionControl.MissionControl.Instance.EncounterLayerGameObject.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
    if (chunkBoundary == null) {
      MissionControl.Main.Logger.LogError("[GetRandomPositionWithinBounds] Cannot find EncounterBoundaryChunkGameLogic. Returning target.");
      return target;
    }
    Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();

    Vector3 randomRecPosition = boundaryRec.GetRandomPositionFromTarget(target, maxDistance);
    return randomRecPosition.GetClosestHexLerpedPointOnGrid();
  }

  public static Vector3 GetRandomPositionFromTarget(Vector3 targPosition, float minDistance, float maxDistance, int attemptCount = 0) {
    if (attemptCount > 5) {
      maxDistance = maxDistance * 2;
    }

    Vector3 targetPosition = targPosition.GetClosestHexLerpedPointOnGrid();
    Vector3 randomPositionWithinBounds = GetRandomPositionWithinBounds(targPosition, maxDistance);

    if (attemptCount > 10) {
      return randomPositionWithinBounds;
    }

    if (!IsWithinBoundedDistanceOfTarget(targetPosition, randomPositionWithinBounds, minDistance, maxDistance)) {
      attemptCount++;
      MissionControl.Main.LogDebugWarning($"[GetRandomPositionFromTarget] Position is not within bounds. Getting new random position");
      return GetRandomPositionFromTarget(targetPosition, minDistance, maxDistance, attemptCount);
    } else {
      return randomPositionWithinBounds;
    }
  }

  // Gets a random position within a cone/sector in the specified direction
  // Useful for biasing position selection away from enemies
  public static Vector3 GetRandomPositionInDirection(Vector3 origin, Vector3 direction, float minDistance, float maxDistance, float coneAngleDegrees = 90f) {
    EncounterBoundaryChunkGameLogic chunkBoundary = MissionControl.MissionControl.Instance.EncounterLayerGameObject.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
    if (chunkBoundary == null) {
      MissionControl.Main.Logger.LogError("[GetRandomPositionInDirection] Cannot find EncounterBoundaryChunkGameLogic. Returning origin.");
      return origin;
    }
    Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();

    // Zero out Y FIRST, then normalize to ensure correct magnitude
    Vector3 normalizedDirection = direction;
    normalizedDirection.y = 0;
    normalizedDirection = normalizedDirection.normalized;

    // Pick a random distance
    float distance = UnityEngine.Random.Range(minDistance, maxDistance);

    // Pick a random angle within the cone
    float halfConeAngle = coneAngleDegrees / 2f;
    float randomAngle = UnityEngine.Random.Range(-halfConeAngle, halfConeAngle);

    // Rotate the direction by the random angle (around Y axis)
    float radians = randomAngle * Mathf.Deg2Rad;
    float cos = Mathf.Cos(radians);
    float sin = Mathf.Sin(radians);
    Vector3 rotatedDirection = new Vector3(
      (normalizedDirection.x * cos) - (normalizedDirection.z * sin),
      0,
      (normalizedDirection.x * sin) + (normalizedDirection.z * cos)
    );

    // Calculate the position
    Vector3 position = origin + (rotatedDirection * distance);

    // Clamp to boundary
    position.x = Mathf.Clamp(position.x, boundaryRec.xMin, boundaryRec.xMax);
    position.z = Mathf.Clamp(position.z, boundaryRec.yMin, boundaryRec.yMax);

    // Check if clamping broke the distance requirements (near map edges)
    Vector3 toPosition = position - origin;
    toPosition.y = 0;
    float actualDistance = toPosition.magnitude;
    if (actualDistance < minDistance || actualDistance > maxDistance) {
      MissionControl.Main.LogDebugWarning($"[GetRandomPositionInDirection] Boundary clamping broke distance requirements ({actualDistance} not in [{minDistance}, {maxDistance}]). Returning origin to trigger retry.");
      return origin; // Return origin to signal retry needed
    }

    return position.GetClosestHexLerpedPointOnGrid();
  }

  public static bool IsWithinBoundedDistanceOfTarget(Vector3 origin, Vector3 target, float minDistance, float maxDistance) {
    Vector3 vectorToTarget = target - origin;
    vectorToTarget.y = 0;
    float distance = vectorToTarget.magnitude;
    if ((distance >= minDistance) && (distance <= maxDistance)) return true;
    MissionControl.Main.LogDebugWarning($"[IsWithinBoundedDistanceOfTarget] Distance is {distance} and so not within bounds. Getting new random position");
    return false;
  }

  public static Vector3 CalculateCentroidOfActors(List<AbstractActor> actors, List<AbstractActor> avoidActors = null) {
    // Filter out dead actors and those without GameRep - only living units with valid rep should affect the centroid
    List<AbstractActor> livingActors = actors.FindAll(a => !a.IsDead && a.GameRep != null);

    if (livingActors.Count == 0) {
      MissionControl.Main.Logger.LogWarning("[CalculateCentroidOfActors] No living actors found in primary list. Returning Vector3.zero.");
      return Vector3.zero;
    }

    // Calculate player centroid (living units only)
    float totalMass = 0;
    Vector3 playerCentroid = Vector3.zero;

    for (int i = 0; i < livingActors.Count; i++) {
      AbstractActor actor = livingActors[i];
      Vector3 position = actor.GameRep.transform.position;
      position.y = 0; // Zero out Y to prevent height pollution

      float weight = 2; // placeholder weighting - maybe replace by tonnage?

      playerCentroid += position * weight;
      totalMass += weight;
    }

    playerCentroid /= totalMass;

    // If no avoid actors, just return player centroid
    if (avoidActors == null || avoidActors.Count == 0) {
      return playerCentroid.GetClosestHexLerpedPointOnGrid();
    }

    // Filter out dead enemies and those without GameRep - only living enemies with valid rep should be avoided
    List<AbstractActor> livingEnemies = avoidActors.FindAll(a => !a.IsDead && a.GameRep != null);

    // If all enemies are dead, just return player centroid
    if (livingEnemies.Count == 0) {
      MissionControl.Main.LogDebug("[CalculateCentroidOfActors] No living enemies found. Using player centroid only.");
      return playerCentroid.GetClosestHexLerpedPointOnGrid();
    }

    // Calculate enemy centroid (living units only)
    float enemyTotalMass = 0;
    Vector3 enemyCentroid = Vector3.zero;

    for (int i = 0; i < livingEnemies.Count; i++) {
      AbstractActor actor = livingEnemies[i];
      Vector3 position = actor.GameRep.transform.position;
      position.y = 0; // Zero out Y to prevent height pollution

      float weight = 2; // Use same weight as players for balance

      enemyCentroid += position * weight;
      enemyTotalMass += weight;
    }

    enemyCentroid /= enemyTotalMass;

    // Calculate vector from enemy centroid toward player centroid
    Vector3 awayFromEnemies = playerCentroid - enemyCentroid;

    // If player and enemy centroids are very close, pick a random direction
    if (awayFromEnemies.magnitude < 10f) {
      MissionControl.Main.LogDebug("[CalculateCentroidOfActors] Player and enemy centroids are very close. Using player centroid.");
      return playerCentroid.GetClosestHexLerpedPointOnGrid();
    }

    // Normalize and extend beyond player position by a reasonable distance (200 units)
    Vector3 direction = awayFromEnemies.normalized;
    Vector3 safeCentroid = playerCentroid + (direction * 200f);

    return safeCentroid.GetClosestHexLerpedPointOnGrid();
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

    // Add center
    MapEncounterLayerDataCell layerDataCell = GetOrCreateEncounterLayerDataCell(cellX, cellZ);
    cells.Add(layerDataCell);

    float bottom = colliderCenter.x - colliderExtents.x;
    float top = colliderCenter.x + colliderExtents.x;
    float left = colliderCenter.z - colliderExtents.z;
    float right = colliderCenter.z + colliderExtents.z;

    for (float i = bottom; i <= top; i += 0.5f) {
      for (float j = left; j <= right; j += 0.5f) {
        cellX = encounterLayerData.GetXIndex(i);
        cellZ = encounterLayerData.GetZIndex(j);

        layerDataCell = GetOrCreateEncounterLayerDataCell(cellX, cellZ);

        if (layerDataCell != null) cells.Add(layerDataCell);
      }
    }

    return cells;
  }

  public static MapEncounterLayerDataCell GetOrCreateEncounterLayerDataCell(int x, int z) {
    // Add a safe get cell
    if (MissionControl.MissionControl.Instance.EncounterLayerData.IsWithinBounds(x, z)) {
      MapEncounterLayerDataCell encounterLayerDataCell = MissionControl.MissionControl.Instance.EncounterLayerData.GetSafeCellAt(x, z);
      if (encounterLayerDataCell == null) encounterLayerDataCell = new MapEncounterLayerDataCell();
      if (encounterLayerDataCell.relatedTerrainCell == null) encounterLayerDataCell.relatedTerrainCell = UnityGameInstance.BattleTechGame.Combat.MapMetaData.SafeGetCellAt(x, z);

      // Seems the x, z are reversed in the BT source, so better keep it the same
      MissionControl.MissionControl.Instance.EncounterLayerData.mapEncounterLayerDataCells[z, x] = encounterLayerDataCell;
      return encounterLayerDataCell;
    }
    return null;
  }
}