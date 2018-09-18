using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class SpawnLanceMembersAroundTarget : SpawnLanceLogic {
    private float minDistanceFromTarget = 100f;
    private float maxDistanceFromTarget = 150f;
    private float minDistanceToSpawnFromInvalidSpawn = 30f;
    private List<Vector3> invalidSpawnLocations = new List<Vector3>();

    public SpawnLanceMembersAroundTarget(GameObject lance, GameObject orientationTarget, LookDirection lookDirection) : base() {
      Spawn(lance, orientationTarget, lookDirection);
    }

    public SpawnLanceMembersAroundTarget(GameObject lance, GameObject orientationTarget, LookDirection lookDirection, float minDistance, float maxDistance) : base() {
      minDistanceFromTarget = minDistance;
      maxDistanceFromTarget = maxDistance;
      Spawn(lance, orientationTarget, lookDirection);
    }

    public void Spawn(GameObject lance, GameObject orientationTarget, LookDirection lookDirection) {
      Main.Logger.Log($"[SpawnLanceMembersAroundTarget] For {lance.name}");
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");
      foreach (GameObject spawnPoint in spawnPoints) {
        SpawnLanceMember(spawnPoint, orientationTarget, lookDirection);
      }
      invalidSpawnLocations.Clear();
    }

    public void SpawnLanceMember(GameObject spawnPoint, GameObject orientationTarget, LookDirection lookDirection) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Vector3 spawnPointPosition = combatState.HexGrid.GetClosestPointOnGrid(spawnPoint.transform.position);
      Vector3 newSpawnPosition = GetRandomPositionFromTarget(orientationTarget, minDistanceFromTarget, maxDistanceFromTarget);
      newSpawnPosition.y = combatState.MapMetaData.GetLerpedHeightAt(newSpawnPosition);

      if (!IsWithinDistanceOfInvalidPosition(newSpawnPosition)) {      
        spawnPoint.transform.position = newSpawnPosition;
        invalidSpawnLocations.Add(newSpawnPosition);

        if (lookDirection == LookDirection.TOWARDS_TARGET) {
          RotateToTarget(spawnPoint, orientationTarget);
        } else {
          RotateAwayFromTarget(spawnPoint, orientationTarget);
        }

        if (!IsSpawnValid(spawnPoint, orientationTarget)) {
          SpawnLanceMember(spawnPoint, orientationTarget, lookDirection);
        } else {
          Main.Logger.Log("[SpawnLanceMembersAroundTarget] Lance member spawn complete");
        }
      } else {
        Main.Logger.LogWarning("[SpawnLanceMembersAroundTarget] Cannot spawn a lance member on an invalid spawn. Finding new spawn point.");
        SpawnLanceMember(spawnPoint, orientationTarget, lookDirection);
      }
    }

    private bool IsWithinDistanceOfInvalidPosition(Vector3 newSpawn) {
      foreach (Vector3 invalidSpawn in invalidSpawnLocations) {
        Vector3 vectorToInvalidSpawn = newSpawn - invalidSpawn;
        vectorToInvalidSpawn.y = 0;
        float disatanceToInvalidSpawn = vectorToInvalidSpawn.magnitude;
        if (disatanceToInvalidSpawn < minDistanceToSpawnFromInvalidSpawn) return true;
      }
      return false;
    }
  }
}