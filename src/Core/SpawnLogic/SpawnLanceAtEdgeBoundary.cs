using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class SpawnLanceAtEdgeOfBoundary : SpawnLogic {
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
      RotateLanceToTarget(lance, orientationTarget);

      if (!AreLanceMemberSpawnsValid(lance, orientationTarget)) {
        Spawn(lance, orientationTarget);
      } else {
        Main.Logger.Log("[SpawnLanceAtEdgeOfBoundary] Lance spawn complete");
      }
    }

    private bool AreLanceMemberSpawnsValid(GameObject lance, GameObject orientationTarget) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");

      foreach (GameObject spawnPoint in spawnPoints) {
        Vector3 spawnPointPosition = combatState.HexGrid.GetClosestPointOnGrid(spawnPoint.transform.position);
        spawnPointPosition.y = combatState.MapMetaData.GetLerpedHeightAt(spawnPointPosition);

        Vector3 checkTarget = combatState.HexGrid.GetClosestPointOnGrid(orientationTarget.transform.position);
        checkTarget.y = combatState.MapMetaData.GetLerpedHeightAt(checkTarget);
        
        if (!PathFinderManager.GetInstance().IsSpawnValid(spawnPointPosition, checkTarget)) {
          Main.Logger.LogWarning("[AreLanceMemberSpawnsValid] Lance member spawn path to first objective is blocked. Select a new lance spawn point");
          return false;
        }
      }

      PathFinderManager.GetInstance().Reset();
      return true;
    }

    private void RotateLanceToTarget(GameObject lance, GameObject target) {
      Vector3 targetPosition = target.transform.position;
      lance.transform.LookAt(new Vector3(targetPosition.x, lance.transform.position.y, targetPosition.z));
    }
  }
}