using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class SpawnLanceAtEdgeOfBoundary : SpawnLogic {
    public SpawnLanceAtEdgeOfBoundary(GameObject lanceGameObject) : base() {
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] For {lanceGameObject.name}");
      SpawnManager spawnManager = SpawnManager.GetInstance();
      GameObject chunkBoundaryRect = spawnManager.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = boundaryLogic.GetRect();

      Vector3 xzEdge = boundaryRec.CalculateRandomXZEdge(boundary.transform.position);

      Vector3 lancePosition = lanceGameObject.transform.position;
      Vector3 newSpawnPosition = new Vector3(xzEdge.x, lancePosition.y, xzEdge.z);
      newSpawnPosition.y = UnityGameInstance.BattleTechGame.Combat.MapMetaData.GetLerpedHeightAt(newSpawnPosition);
      lanceGameObject.transform.position = newSpawnPosition;
    }

    private void RotateLanceMembersToTarget(GameObject target) {

    }
  }
}