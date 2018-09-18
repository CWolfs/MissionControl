using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace SpawnVariation.Logic {
  public class SpawnLogic {
    public enum LookDirection { TOWARDS_TARGET, AWAY_FROM_TARGET };

    public SpawnLogic() { }

    protected void RotateToTarget(GameObject focus, GameObject target) {
      Vector3 targetPosition = target.transform.position;
      focus.transform.LookAt(new Vector3(targetPosition.x, focus.transform.position.y, targetPosition.z));
    }

    protected void RotateAwayFromTarget(GameObject focus, GameObject target) {
      Vector3 targetPosition = target.transform.position;
      Vector3 focusPosition = focus.transform.position;
      Vector3 lookAtPosition = focusPosition - ( targetPosition - focusPosition);

      Vector3 lookAtTargetPosition = new Vector3(lookAtPosition.x, focusPosition.y, lookAtPosition.z);
      focus.transform.LookAt(lookAtTargetPosition);
    }

    protected Vector3 GetRandomPositionFromTarget(GameObject target, float minDistance, float maxDistance) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      // UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
      Vector3 targetPosition = target.transform.position;
    
      float xSignSelection = (UnityEngine.Random.value < 0.5f) ? -1f : 1f;
      float zSignSelection = (UnityEngine.Random.value < 0.5f) ? -1f : 1f;
      float xValue = UnityEngine.Random.Range(minDistance / 1.5f, maxDistance / 1.5f) * xSignSelection;
      float zValue = UnityEngine.Random.Range(minDistance  / 1.5f, maxDistance / 1.5f) * zSignSelection;

      Vector3 randomPositionFromTarget = new Vector3(targetPosition.x + xValue, 0, targetPosition.z + zValue);
      float yValue = combatState.MapMetaData.GetLerpedHeightAt(randomPositionFromTarget);
      randomPositionFromTarget.y = yValue;

      if (!IsWithinBoundedDistanceOfTarget(targetPosition, randomPositionFromTarget, minDistance, maxDistance)) {
        return GetRandomPositionFromTarget(target, minDistance, maxDistance);
      } else {
        return randomPositionFromTarget;
      }
    }

    private bool IsWithinBoundedDistanceOfTarget(Vector3 origin, Vector3 target, float minDistance, float maxDistance) {
      Vector3 vectorToTarget = target - origin;
      vectorToTarget.y = 0;
      float distance = vectorToTarget.magnitude;
      if ((distance > minDistance) && (distance < maxDistance)) return true;
      Main.Logger.LogWarning($"[IsWithinBoundedDistanceOfTarget] Distance is {distance} and so not within bounds. Getting new random position");
      return false;
    }

    protected bool IsSpawnValid(GameObject spawnPoint, GameObject orientationTarget) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Vector3 spawnPointPosition = combatState.HexGrid.GetClosestPointOnGrid(spawnPoint.transform.position);
      spawnPointPosition.y = combatState.MapMetaData.GetLerpedHeightAt(spawnPointPosition);

      Vector3 checkTarget = combatState.HexGrid.GetClosestPointOnGrid(orientationTarget.transform.position);
      checkTarget.y = combatState.MapMetaData.GetLerpedHeightAt(checkTarget);
      
      if (!PathFinderManager.GetInstance().IsSpawnValid(spawnPointPosition, checkTarget)) {
        Main.Logger.LogWarning("[IsSpawnValid] Spawn path to first objective is blocked. Select a new spawn point");
        return false;
      }
      
      PathFinderManager.GetInstance().Reset();
      return true;
    }
  }
}