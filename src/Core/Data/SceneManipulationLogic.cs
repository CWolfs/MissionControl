using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

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
      return GetRandomPositionFromTarget(target.transform.position, minDistance, maxDistance);
    }

    protected Vector3 GetRandomPositionFromTarget(Vector3 targPosition, float minDistance, float maxDistance) {
      Vector3 targetPosition = targPosition.GetClosestHexLerpedPointOnGrid();
      Vector3 randomPositionWithinBounds = GetRandomPositionWithinBounds(targPosition, maxDistance);

      if (!IsWithinBoundedDistanceOfTarget(targetPosition, randomPositionWithinBounds, minDistance, maxDistance)) {
        return GetRandomPositionFromTarget(targetPosition, minDistance, maxDistance);
      } else {
        return randomPositionWithinBounds;
      }
    }

    public Vector3 GetRandomPositionWithinBounds(Vector3 target, float maxDistance) {
      MissionControl EncounterManager = MissionControl.Instance;
      GameObject chunkBoundaryRect = EncounterManager.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();

      Vector3 randomRecPosition = boundaryRec.GetRandomPositionFromTarget(target, maxDistance);
      return randomRecPosition.GetClosestHexLerpedPointOnGrid();
    }

    public Vector3 GetRandomPositionWithinBounds() {
      MissionControl EncounterManager = MissionControl.Instance;
      GameObject chunkBoundaryRect = EncounterManager.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();

      Vector3 randomRecPosition = boundaryRec.GetRandomPosition();
      return randomRecPosition.GetClosestHexLerpedPointOnGrid();
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
      if (distance >= minDistance) return true;
      Main.Logger.LogWarning($"[IsWithinBoundedDistanceOfTarget] Distance is {distance} and so not within bounds. Getting new random position");
      return false;  
    }
  }
}