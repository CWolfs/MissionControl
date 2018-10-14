using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public abstract class SceneManipulationLogic : LogicBlock {
    public enum LookDirection { TOWARDS_TARGET, AWAY_FROM_TARGET };

    protected EncounterRules EncounterRules { get; set; }

    public SceneManipulationLogic(EncounterRules encounterRules) {
      this.Type = LogicType.SCENE_MANIPULATION;
      EncounterRules = encounterRules;
    }

    protected abstract void GetObjectReferences();

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
      Vector3 targetPosition = target.transform.position;

      float xSignSelection = (UnityEngine.Random.value < 0.5f) ? -1f : 1f;
      float zSignSelection = (UnityEngine.Random.value < 0.5f) ? -1f : 1f;

      float randomBuffer = UnityEngine.Random.Range(1f, 2f);

      float xValue = UnityEngine.Random.Range(minDistance / randomBuffer, maxDistance / randomBuffer) * xSignSelection;
      float zValue = UnityEngine.Random.Range(minDistance  / randomBuffer, maxDistance / randomBuffer) * zSignSelection;

      Vector3 randomPositionFromTarget = new Vector3(targetPosition.x + xValue, 0, targetPosition.z + zValue);
      randomPositionFromTarget = combatState.HexGrid.GetClosestPointOnGrid(randomPositionFromTarget);
      float yValue = combatState.MapMetaData.GetLerpedHeightAt(randomPositionFromTarget);
      randomPositionFromTarget.y = yValue;

      if (!IsWithinBoundedDistanceOfTarget(targetPosition, randomPositionFromTarget, minDistance, maxDistance)) {
        return GetRandomPositionFromTarget(target, minDistance, maxDistance);
      } else {
        return randomPositionFromTarget;
      }
    }

    public bool IsWithinBoundedDistanceOfTarget(Vector3 origin, Vector3 target, float minDistance, float maxDistance) {
      Vector3 vectorToTarget = target - origin;
      vectorToTarget.y = 0;
      float distance = vectorToTarget.magnitude;
      if ((distance > minDistance) && (distance < maxDistance)) return true;
      Main.Logger.LogWarning($"[IsWithinBoundedDistanceOfTarget] Distance is {distance} and so not within bounds. Getting new random position");
      return false;
    }

    public bool IsWithinBoundedDistanceOfTarget(Vector3 origin, Vector3 target, float minDistance) {
      Vector3 vectorToTarget = target - origin;
      vectorToTarget.y = 0;
      float distance = vectorToTarget.magnitude;
      if (distance > minDistance) return true;
      Main.Logger.LogWarning($"[IsWithinBoundedDistanceOfTarget] Distance is {distance} and so not within bounds. Getting new random position");
      return false;  
    }
  }
}