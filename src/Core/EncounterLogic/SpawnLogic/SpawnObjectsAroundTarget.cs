using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class SpawnObjectsAroundTarget : SpawnLogic {
    private LogicState state;

    private List<string> configuredObjectKeys = new List<string>();
    private List<string> objectKeys = new List<string>();
    private List<string> orientationTargetKeys = new List<string>();
    private Dictionary<string, string> keyLookup = new Dictionary<string, string>();
    private string defaultOrientationTargetKey = "";

    private List<GameObject> objectGos = new List<GameObject>();
    private Dictionary<string, GameObject> orientationTargets = new Dictionary<string, GameObject>();
    private Dictionary<GameObject, Vector3> validOrientationTargetPositions = new Dictionary<GameObject, Vector3>();

    private float minDistanceFromTarget = 50f;
    private float maxDistanceFromTarget = 150f;
    private LookDirection lookDirection = LookDirection.TOWARDS_TARGET;

    private int AttemptCountMax { get; set; } = 3;
    private int AttemptCount { get; set; } = 0;
    private int TotalAttemptMax { get; set; } = 3;
    private int TotalAttemptCount { get; set; } = 0;

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, string objectKey, string orientationTargetKey, LookDirection lookDirection) : base(encounterRules) {
      this.configuredObjectKeys = new List<string> { objectKey };
      this.defaultOrientationTargetKey = orientationTargetKey;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, string objectKey, string orientationTargetKey, LookDirection lookDirection, float minDistance, float maxDistance) : base(encounterRules) {
      this.configuredObjectKeys = new List<string> { objectKey };
      this.defaultOrientationTargetKey = orientationTargetKey;
      this.minDistanceFromTarget = minDistance;
      this.maxDistanceFromTarget = maxDistance;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, List<string> objectKeys, string orientationTargetKey, LookDirection lookDirection) : base(encounterRules) {
      this.configuredObjectKeys = new List<string>(objectKeys);
      this.defaultOrientationTargetKey = orientationTargetKey;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, List<string> objectKeys, string orientationTargetKey, LookDirection lookDirection, float minDistance, float maxDistance) : base(encounterRules) {
      this.configuredObjectKeys = new List<string>(objectKeys);
      this.defaultOrientationTargetKey = orientationTargetKey;
      this.minDistanceFromTarget = minDistance;
      this.maxDistanceFromTarget = maxDistance;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, LogicState state, LookDirection lookDirection) : base(encounterRules) {
      this.state = state;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, LogicState state, LookDirection lookDirection, float minDistance, float maxDistance) : base(encounterRules) {
      this.state = state;
      this.minDistanceFromTarget = minDistance;
      this.maxDistanceFromTarget = maxDistance;
      this.lookDirection = lookDirection;
    }

    public override void Run(RunPayload payload) {
      if (!GetObjectReferences()) return;

      // GUARD: Every object should have a key and orientation target. Otherwise something went wrong.
      if (objectKeys.Count != objectGos.Count) {
        Main.Logger.LogError($"[SpawnObjectsAroundTarget] Objects do not have a matching set of 'object keys' for this spawner to work correctly.");
        return;
      }

      if (orientationTargetKeys.Count != objectGos.Count) {
        Main.Logger.LogError($"[SpawnObjectsAroundTarget] Objects do not have a matching set of 'orientation target keys' for this spawner to work correctly.");
        return;
      }

      for (int i = 0; i < objectGos.Count; i++) {
        GameObject objectGo = objectGos[i];
        if (objectGo == null) {
          Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] Found a null object. Skipping.'");
          continue;
        }
        Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] Attempting for '{objectGo.name}'");


        string objectKey = (objectKeys.Count > i) ? objectKeys[i] : "INVALID";
        Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] With key '{objectKey}'");
        string orientationTargetKey = orientationTargetKeys[i];
        Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] And orientation target '{orientationTargetKey}'");

        if (!orientationTargets.ContainsKey(orientationTargetKey) || orientationTargets[orientationTargetKey] == null) {
          Main.Logger.LogError($"[SpawnObjectsAroundTarget] Orientation target with key '{orientationTargetKey}' does not exist. This is required for this spawner to work correctly.");
          return;
        }

        GameObject orientationTarget = orientationTargets[orientationTargetKey];
        Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] Using orientation target key '{orientationTargetKey}' and Go name '{orientationTarget.transform.name}'");

        SpawnObjectAroundTarget(objectGo, orientationTarget);
      }
    }

    private void SpawnObjectAroundTarget(GameObject objectGo, GameObject orientationTarget) {
      SaveSpawnPosition(objectGo);

      Vector3 validOrientationTargetPosition = GetValidOrientationTargetPosition(orientationTarget);

      while (TotalAttemptCount < TotalAttemptMax) {
        Vector3 newSpawnPosition = GetRandomPositionFromTarget(validOrientationTargetPosition, minDistanceFromTarget, maxDistanceFromTarget);
        newSpawnPosition = GetClosestValidPathFindingHex(objectGo, newSpawnPosition, $"NewRandomSpawnPositionFromOrientationTarget.{orientationTarget.name}", 2);

        if (MissionControl.Instance.EncounterLayerData.IsInEncounterBounds(newSpawnPosition)) {
          objectGo.transform.position = newSpawnPosition;

          if (lookDirection == LookDirection.TOWARDS_TARGET) {
            RotateToTarget(objectGo, orientationTarget);
          } else {
            RotateAwayFromTarget(objectGo, orientationTarget);
          }

          if (IsSpawnValid(objectGo, validOrientationTargetPosition)) {
            Main.Logger.Log("[SpawnObjectsAroundTarget] Object spawn complete");
            ResetAttempts();
            return;
          }
        } else {
          Main.LogDebugWarning("[SpawnObjectsAroundTarget] Selected object spawn point is outside of the boundary. Select a new object spawn point.");
        }

        CheckAttempts();
      }

      Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] Could not find a valid spawn for '{objectGo.name}' after '{TotalAttemptCount}' attempts. Restoring its original position.");
      RestoreSpawnPosition(objectGo);
      ResetAttempts();
    }

    private Vector3 GetValidOrientationTargetPosition(GameObject orientationTarget) {
      if (validOrientationTargetPositions.ContainsKey(orientationTarget)) {
        Vector3 validPosition = validOrientationTargetPositions[orientationTarget];
        Main.LogDebug($"[SpawnObjectsAroundTarget] Reusing cached orientation target of '{orientationTarget.name}' at '{validPosition}'.");
        return validPosition;
      }

      Main.LogDebug($"[SpawnObjectsAroundTarget] Orientation target of '{orientationTarget.name}' at '{orientationTarget.transform.position}'. Attempting to get closest valid path finding hex.");
      Vector3 validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget, orientationTarget.transform.position, $"OrientationTarget.{orientationTarget.name}");
      validOrientationTargetPositions[orientationTarget] = validOrientationTargetPosition;

      return validOrientationTargetPosition;
    }

    private void CheckAttempts() {
      AttemptCount++;
      TotalAttemptCount++;

      if (AttemptCount > AttemptCountMax) {
        AttemptCount = 0;
        Main.LogDebug($"[SpawnObjectsAroundTarget] Cannot find a suitable object spawn within the boundaries of {minDistanceFromTarget} and {maxDistanceFromTarget}. Widening search");
        minDistanceFromTarget -= 10;
        if (minDistanceFromTarget <= 10) minDistanceFromTarget = 10;
        maxDistanceFromTarget += 25;
      }
    }

    private void ResetAttempts() {
      AttemptCount = 0;
      TotalAttemptCount = 0;
    }

    protected override bool GetObjectReferences() {
      objectKeys.Clear();
      orientationTargetKeys.Clear();
      keyLookup.Clear();
      objectGos.Clear();
      orientationTargets.Clear();

      if (state != null) {
        List<string[]> extraLanceKeys = (List<string[]>)state.GetObject("ExtraLanceSpawnKeys");
        for (int i = 0; i < extraLanceKeys.Count; i++) {
          string[] keys = extraLanceKeys[i];

          if (keys != null && keys.Length > 0) {
            if (keys.Length == 2) {
              string objectKey = keys[0];
              string orientationObjectKey = keys[1];

              // Check if the key object and orientation object really exist
              if (objectKey == null || objectKey == "") {
                Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] GameObject has a null or empty object key. This is unexpected.");
                continue;
              }

              GameObject objectKeyGO = GameObject.Find(objectKey.Substring(objectKey.IndexOf(".") + 1));
              if (objectKeyGO == null) {
                Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] GameObject with key '{objectKey}' was not found. This is unexpected.");
                continue;
              }

              if (orientationObjectKey == null || orientationObjectKey == "") {
                Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] GameObject has a null or empty orientation object key. This is unexpected.");
                continue;
              }

              GameObject orientationObjectKeyGO = GameObject.Find(orientationObjectKey.Substring(orientationObjectKey.IndexOf(".") + 1));
              if (orientationObjectKeyGO == null) {
                Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] GameObject for orientation target with key '{orientationObjectKeyGO}' was not found. This is unexpected.");
                continue;
              }

              objectKeys.Add(objectKey);
              orientationTargetKeys.Add(orientationObjectKey);
              keyLookup[objectKey] = orientationObjectKey;
            } else {
              Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] ExtraLanceSpawnKeys keyset does not provide exactly a key and orientation key.");
            }
          } else {
            Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] ExtraLanceSpawnKeys provides a first key set but the keyset is null or empty");
          }
        }
      } else {
        for (int i = 0; i < configuredObjectKeys.Count; i++) {
          string objectKey = configuredObjectKeys[i];

          objectKeys.Add(objectKey);
          orientationTargetKeys.Add(defaultOrientationTargetKey);
          keyLookup[objectKey] = defaultOrientationTargetKey;
        }
      }

      for (int i = 0; i < objectKeys.Count; i++) {
        GameObject objectGoShell;
        this.EncounterRules.ObjectLookup.TryGetValue(objectKeys[i], out objectGoShell);
        objectGos.Add(objectGoShell);
      }

      for (int i = 0; i < orientationTargetKeys.Count; i++) {
        GameObject orientationTargetGoShell;
        this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKeys[i], out orientationTargetGoShell);
        orientationTargets[orientationTargetKeys[i]] = orientationTargetGoShell;
      }

      if (defaultOrientationTargetKey != "") {
        GameObject defaultOrientationTargetGoShell;
        this.EncounterRules.ObjectLookup.TryGetValue(defaultOrientationTargetKey, out defaultOrientationTargetGoShell);
        orientationTargets[defaultOrientationTargetKey] = defaultOrientationTargetGoShell;
      }

      return true;
    }
  }
}
