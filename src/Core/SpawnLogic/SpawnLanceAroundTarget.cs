using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class SpawnLanceAroundTarget : SpawnLanceLogic {
    private float minDistanceFromTarget = 50f;
    private float maxDistanceFromTarget = 150f;

    public SpawnLanceAroundTarget(GameObject lance, GameObject orientationTarget, LookDirection lookDirection) : base() {
      Spawn(lance, orientationTarget, lookDirection);
    }

    public SpawnLanceAroundTarget(GameObject lance, GameObject orientationTarget, LookDirection lookDirection, float minDistance, float maxDistance) : base() {
      minDistanceFromTarget = minDistance;
      maxDistanceFromTarget = maxDistance;
      Spawn(lance, orientationTarget, lookDirection);
    }

    public void Spawn(GameObject lance, GameObject orientationTarget, LookDirection lookDirection) {
      Main.Logger.Log($"[SpawnLanceAroundTarget] For {lance.name}");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      SpawnManager spawnManager = SpawnManager.GetInstance();

      Vector3 lancePosition = lance.transform.position;
      Vector3 newSpawnPosition = GetRandomPositionFromTarget(lance, minDistanceFromTarget, maxDistanceFromTarget);
      newSpawnPosition.y = combatState.MapMetaData.GetLerpedHeightAt(newSpawnPosition);
      lance.transform.position = newSpawnPosition;

      if (lookDirection == LookDirection.TOWARDS_TARGET) {
        RotateToTarget(lance, orientationTarget);
      } else {
        RotateAwayFromTarget(lance, orientationTarget);
      }

      if (!AreLanceMemberSpawnsValid(lance, orientationTarget)) {
        Spawn(lance, orientationTarget, lookDirection);
      } else {
        Main.Logger.Log("[SpawnLanceAroundTarget] Lance spawn complete");
      }
    }
  }
}