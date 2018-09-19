using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class SpawnLanceAtEdgeOfBoundary : SpawnLanceLogic {
    private int AttemptCount { get; set; } = 0;

    public SpawnLanceAtEdgeOfBoundary(GameObject lance, GameObject orientationTarget) : base() {
      Spawn(lance, orientationTarget, RectExtensions.RectEdge.ANY);
    }

    public void Spawn(GameObject lance, GameObject orientationTarget, RectExtensions.RectEdge edge) {
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] For {lance.name}");
      AttemptCount++;
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      SpawnManager spawnManager = SpawnManager.GetInstance();
      GameObject chunkBoundaryRect = spawnManager.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();

      // Vector3 xzEdge = boundaryRec.CalculateRandomXZEdge(boundary.transform.position);
      RectEdgePosition xzEdge = boundaryRec.CalculateRandomXZEdge(boundary.transform.position, edge);

      Vector3 lancePosition = lance.transform.position;
      Vector3 newSpawnPosition = new Vector3(xzEdge.Position.x, lancePosition.y, xzEdge.Position.z);
      newSpawnPosition.y = combatState.MapMetaData.GetLerpedHeightAt(newSpawnPosition);

      lance.transform.position = newSpawnPosition;
      RotateToTarget(lance, orientationTarget);

      if (!AreLanceMemberSpawnsValid(lance, orientationTarget)) {
        if (AttemptCount > 10) {  // Attempt to spawn on the selected edge. If it's not possible, select another edge
          Spawn(lance, orientationTarget, RectExtensions.RectEdge.ANY);
        } else {
          Spawn(lance, orientationTarget, xzEdge.Edge);
        }
      } else {
        Main.Logger.Log("[SpawnLanceAtEdgeOfBoundary] Lance spawn complete");
      }
    }
  }
}