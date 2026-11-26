using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class UnplacedGeneratedLanceMarker : MonoBehaviour { }

  public abstract class SceneManipulationLogic : LogicBlock {
    public enum LookDirection { TOWARDS_TARGET, AWAY_FROM_TARGET };
    private bool hasSavedOriginalPositions = false;
    private Vector3 vanillaLanceSpawnPosition = Vector3.zero;
    private Quaternion vanillaLanceSpawnRotation = Quaternion.identity;

    // Saved locations
    private List<Vector3> vanillaLanceUnitSpawnPositions = new List<Vector3>();
    private List<Quaternion> vanillaLanceUnitSpawnRotations = new List<Quaternion>();
    private bool hasSavedAttemptFormation = false;
    private Quaternion attemptLanceRotation = Quaternion.identity;
    private List<Vector3> attemptLanceUnitLocalPositions = new List<Vector3>();
    private List<Quaternion> attemptLanceUnitLocalRotations = new List<Quaternion>();
    private Vector3 originalObjectPosition = Vector3.zero;
    private List<Vector3> originalObjectPositions = new List<Vector3>();
    private List<Quaternion> originalObjectRotations = new List<Quaternion>();

    protected EncounterRules EncounterRules { get; set; }

    public SceneManipulationLogic(EncounterRules encounterRules) {
      this.Type = LogicType.SCENE_MANIPULATION;
      EncounterRules = encounterRules;
    }

    protected abstract bool GetObjectReferences();

    protected void SaveSpawnPosition(GameObject objectGo) {
      originalObjectPosition = objectGo.transform.position;
    }

    protected void RestoreSpawnPosition(GameObject objectGo) {
      if (originalObjectPosition != Vector3.zero) objectGo.transform.position = originalObjectPosition;
    }

    protected void SaveSpawnPositions(List<GameObject> objectGos) {
      if (originalObjectPositions.Count > 0) return;

      for (int i = 0; i < objectGos.Count; i++) {
        originalObjectPositions.Add(objectGos[i].transform.position);
        originalObjectRotations.Add(objectGos[i].transform.rotation);
      }
    }

    protected void RestoreSpawnPositions(List<GameObject> objectGos) {
      for (int i = 0; i < objectGos.Count && i < originalObjectPositions.Count; i++) {
        objectGos[i].transform.position = originalObjectPositions[i];
        if (i < originalObjectRotations.Count) {
          objectGos[i].transform.rotation = originalObjectRotations[i];
        }
      }
    }

    protected void SaveSpawnPositions(GameObject lance) {
      if (!hasSavedOriginalPositions && (vanillaLanceSpawnPosition == Vector3.zero) && (vanillaLanceUnitSpawnPositions.Count <= 0)) {
        hasSavedOriginalPositions = true;
        vanillaLanceSpawnPosition = lance.transform.position;
        vanillaLanceSpawnRotation = lance.transform.rotation;
        List<GameObject> originalSpawnPoints = lance.FindAllContains("SpawnPoint");
        foreach (GameObject spawn in originalSpawnPoints) {
          vanillaLanceUnitSpawnPositions.Add(spawn.transform.position);
          vanillaLanceUnitSpawnRotations.Add(spawn.transform.rotation);
        }
      }
    }

    protected void RestoreSpawnPositions(GameObject lance) {
      lance.transform.position = vanillaLanceSpawnPosition;
      lance.transform.rotation = vanillaLanceSpawnRotation;
      List<GameObject> originalSpawnPoints = lance.FindAllContains("SpawnPoint");
      for (int i = 0; i < originalSpawnPoints.Count && i < vanillaLanceUnitSpawnPositions.Count; i++) {
        GameObject spawn = originalSpawnPoints[i];
        spawn.transform.position = vanillaLanceUnitSpawnPositions[i];
        if (i < vanillaLanceUnitSpawnRotations.Count) {
          spawn.transform.rotation = vanillaLanceUnitSpawnRotations[i];
        }
      }
    }

    protected void SaveAttemptFormation(GameObject lance) {
      if (hasSavedAttemptFormation) return;

      attemptLanceRotation = lance.transform.rotation;
      attemptLanceUnitLocalPositions.Clear();
      attemptLanceUnitLocalRotations.Clear();
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");
      foreach (GameObject spawn in spawnPoints) {
        attemptLanceUnitLocalPositions.Add(spawn.transform.localPosition);
        attemptLanceUnitLocalRotations.Add(spawn.transform.localRotation);
      }
      hasSavedAttemptFormation = true;
    }

    protected void RestoreAttemptFormation(GameObject lance) {
      if (!hasSavedAttemptFormation) {
        RestoreSpawnPositions(lance);
        return;
      }

      lance.transform.rotation = attemptLanceRotation;
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");
      for (int i = 0; i < spawnPoints.Count && i < attemptLanceUnitLocalPositions.Count; i++) {
        spawnPoints[i].transform.localPosition = attemptLanceUnitLocalPositions[i];
        if (i < attemptLanceUnitLocalRotations.Count) {
          spawnPoints[i].transform.localRotation = attemptLanceUnitLocalRotations[i];
        }
      }
    }

    protected void RestoreLanceMemberSpawnPositions(GameObject lance) {
      List<GameObject> originalSpawnPoints = lance.FindAllContains("SpawnPoint");
      for (int i = 0; i < originalSpawnPoints.Count; i++) {
        GameObject spawn = originalSpawnPoints[i];
        spawn.transform.position = vanillaLanceUnitSpawnPositions[i];
      }
    }

    protected Vector3 GetOriginalSpawnPosition() {
      return vanillaLanceSpawnPosition;
    }

    protected bool HasSavedOriginalPositions() {
      return hasSavedOriginalPositions;
    }

    protected bool IsUnplacedGeneratedLance(GameObject lance) {
      if (lance != null && lance.GetComponent<UnplacedGeneratedLanceMarker>() != null) return true;
      if (!hasSavedOriginalPositions) return false;
      if (vanillaLanceSpawnPosition != Vector3.zero) return false;
      if (vanillaLanceUnitSpawnPositions.Count == 0) return false;

      // Fallback heuristic if a generated lance was constructed without the marker
      bool allOrigin = true;
      bool matchesFactoryDiagonal = true;
      for (int i = 0; i < vanillaLanceUnitSpawnPositions.Count; i++) {
        Vector3 pos = vanillaLanceUnitSpawnPositions[i];
        if (pos != Vector3.zero) allOrigin = false;
        if (pos != new Vector3(24f * i, 0f, 24f * i)) matchesFactoryDiagonal = false;
      }
      return allOrigin || matchesFactoryDiagonal;
    }

    protected void RotateToTarget(GameObject focus, GameObject target) {
      Vector3 targetPosition = target.transform.position;
      focus.transform.LookAt(new Vector3(targetPosition.x, focus.transform.position.y, targetPosition.z));
    }

    protected void RotateLanceMembersToTarget(GameObject lance, GameObject target) {
      foreach (Transform t in lance.transform) {
        RotateToTarget(t.gameObject, target);
      }
    }

    protected void RotateAwayFromTarget(GameObject focus, GameObject target) {
      Vector3 targetPosition = target.transform.position;
      Vector3 focusPosition = focus.transform.position;
      Vector3 lookAtPosition = focusPosition - (targetPosition - focusPosition);

      Vector3 lookAtTargetPosition = new Vector3(lookAtPosition.x, focusPosition.y, lookAtPosition.z);
      focus.transform.LookAt(lookAtTargetPosition);
    }

    protected void RotateLanceMembersAwayFromTarget(GameObject lance, GameObject target) {
      foreach (Transform t in lance.transform) {
        RotateAwayFromTarget(t.gameObject, target);
      }
    }

    protected Vector3 GetRandomPositionFromTarget(GameObject target, float minDistance, float maxDistance) {
      return GetRandomPositionFromTarget(target.transform.position, minDistance, maxDistance);
    }

    protected Vector3 GetRandomPositionFromTarget(Vector3 targPosition, float minDistance, float maxDistance) {
      return SceneUtils.GetRandomPositionFromTarget(targPosition, minDistance, maxDistance);
    }

    public Vector3 GetRandomPositionWithinBounds(Vector3 target, float maxDistance) {
      return SceneUtils.GetRandomPositionWithinBounds(target, maxDistance);
    }

    public Vector3 GetRandomPositionWithinBounds() {
      EncounterBoundaryChunkGameLogic chunkBoundary = MissionControl.Instance.EncounterLayerGameObject.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = chunkBoundary.GetComponentInChildren<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();

      Vector3 randomRecPosition = boundaryRec.GetRandomPosition();
      return randomRecPosition.GetClosestHexLerpedPointOnGrid();
    }

    public bool IsWithinBoundedDistanceOfTarget(Vector3 origin, Vector3 target, float minDistance, float maxDistance) {
      return SceneUtils.IsWithinBoundedDistanceOfTarget(origin, target, minDistance, maxDistance);
    }

    public bool IsBeyondBoundedDistanceOfTarget(Vector3 origin, Vector3 target, float mustBeBeyondDistance) {
      Vector3 vectorToTarget = target - origin;
      vectorToTarget.y = 0;
      float distance = vectorToTarget.magnitude;
      if (distance >= mustBeBeyondDistance) return true;
      Main.LogDebugWarning($"[IsWithinBoundedDistanceOfTarget] Distance is '{distance}' and is within bounds of '{mustBeBeyondDistance}'. Check failure. Getting new random position");
      return false;
    }

    public bool IsWithinBoundedDistanceOfTarget(Vector3 origin, Vector3 target, float mustBeWithinDistance) {
      Vector3 vectorToTarget = target - origin;
      vectorToTarget.y = 0;
      float distance = vectorToTarget.magnitude;
      if (distance <= mustBeWithinDistance) return true;
      Main.LogDebugWarning($"[IsWithinBoundedDistanceOfTarget] Distance is '{distance}' and is beyond bounds of '{mustBeWithinDistance}'. Check failure. Getting new random position");
      return false;
    }

    private static readonly Vector3[] CompactHexFormationOffsets = new Vector3[] {
      new Vector3(0f, 0f, 0f),
      new Vector3(24f, 0f, 0f),
      new Vector3(-12f, 0f, 21f),
      new Vector3(12f, 0f, 21f),
      new Vector3(-24f, 0f, 0f),
      new Vector3(-12f, 0f, -21f),
      new Vector3(12f, 0f, -21f),
      new Vector3(0f, 0f, 42f),
    };

    private static Vector3 GetCompactFormationOffset(int index) {
      if (index < CompactHexFormationOffsets.Length) {
        return CompactHexFormationOffsets[index];
      }
      int overflow = index - CompactHexFormationOffsets.Length;
      return new Vector3(24f * (overflow % 4), 0f, 63f + (21f * (overflow / 4)));
    }

    protected void ClusterLanceMembers(GameObject lance) {
      List<GameObject> originalSpawnPoints = lance.FindAllContains("SpawnPoint");
      if (IsUnplacedGeneratedLance(lance)) {
        for (int i = 0; i < originalSpawnPoints.Count; i++) {
          originalSpawnPoints[i].transform.localPosition = GetCompactFormationOffset(i);
        }
        return;
      }

      List<Vector3> usedPosition = new List<Vector3>();
      for (int i = 0; i < originalSpawnPoints.Count; i++) {
        GameObject spawn = originalSpawnPoints[i];
        Vector3 clusteredSpawnPosition = GetRandomPositionWithinBounds(lance.transform.position, 25f);
        int attempts = 0;
        while (ContainsCompare(usedPosition, clusteredSpawnPosition) && attempts < 30) {
          clusteredSpawnPosition = GetRandomPositionWithinBounds(lance.transform.position, 25f);
          attempts++;
        }

        if (ContainsCompare(usedPosition, clusteredSpawnPosition)) {
          // Switch the whole lance to a distinct deterministic compact formation so no two members can collide
          for (int j = 0; j < originalSpawnPoints.Count; j++) {
            originalSpawnPoints[j].transform.localPosition = GetCompactFormationOffset(j);
          }
          return;
        }

        spawn.transform.position = clusteredSpawnPosition;
        usedPosition.Add(clusteredSpawnPosition);
      }
    }

    private bool ContainsCompare(List<Vector3> list, Vector3 vector) {
      for (int i = 0; i < list.Count; i++) {
        Vector3 listVector = list[i];
        if (listVector == vector) {
          return true;
        }
      }
      return false;
    }
  }
}