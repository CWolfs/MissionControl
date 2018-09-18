using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class SpawnLanceAtEdgeOfBoundary : SpawnLanceLogic {
    public SpawnLanceAtEdgeOfBoundary(GameObject lance, GameObject orientationTarget) : base() {
      Spawn(lance, orientationTarget);
    }

    public void Spawn(GameObject lance, GameObject orientationTarget) {
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] For {lance.name}");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      SpawnManager spawnManager = SpawnManager.GetInstance();
      GameObject chunkBoundaryRect = spawnManager.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = boundaryLogic.GetRect();

      Vector3 xzEdge = boundaryRec.CalculateRandomXZEdge(boundary.transform.position);

      Vector3 lancePosition = lance.transform.position;
      Vector3 newSpawnPosition = new Vector3(xzEdge.x, lancePosition.y, xzEdge.z);
      newSpawnPosition.y = combatState.MapMetaData.GetLerpedHeightAt(newSpawnPosition);

      lance.transform.position = newSpawnPosition;
      RotateToTarget(lance, orientationTarget);

      if (!AreLanceMemberSpawnsValid(lance, orientationTarget)) {
        Spawn(lance, orientationTarget);
      } else {
        Main.Logger.Log("[SpawnLanceAtEdgeOfBoundary] Lance spawn complete");
      }
    }
  }
}