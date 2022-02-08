using UnityEngine;

using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class SpawnObjectAnywhere : SpawnLanceLogic {
    private string objectKey = "";
    private bool useOrientationTarget = false;
    private string orientationTargetKey = "";
    private GameObject target;
    private GameObject orientationTarget;

    private WithinOrBeyondDistanceType distanceCheckType = WithinOrBeyondDistanceType.NONE;
    private float distanceCheck = 400f;
    private float mustBeWithinDistance = 400f;
    private float mustBeBeyondDistance = 400f;

    private int AttemptCountMax { get; set; } = 5;
    private int AttemptCount { get; set; } = 0;
    private int TotalAttemptMax { get; set; } = 5;
    private int TotalAttemptCount { get; set; } = 0;

    private bool inited = false;
    private Vector3 validOrientationTargetPosition;

    public SpawnObjectAnywhere(EncounterRules encounterRules, string objectKey, string orientationTargetKey) : base(encounterRules) {
      this.objectKey = objectKey;
      this.orientationTargetKey = orientationTargetKey;
    }

    public SpawnObjectAnywhere(EncounterRules encounterRules, string objectKey, string orientationTargetKey, float mustBeBeyondDistance) : base(encounterRules) {
      this.objectKey = objectKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.distanceCheckType = WithinOrBeyondDistanceType.MUST_BE_BEYOND;
      this.distanceCheck = mustBeBeyondDistance;
    }

    public SpawnObjectAnywhere(EncounterRules encounterRules, string objectKey, string orientationTargetKey, float mustBeBeyondDistance, float mustBeWithinDistance) : base(encounterRules) {
      this.objectKey = objectKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.distanceCheckType = WithinOrBeyondDistanceType.BOTH;
      this.mustBeBeyondDistance = mustBeBeyondDistance;
      this.mustBeWithinDistance = mustBeWithinDistance;
      this.distanceCheck = mustBeBeyondDistance;
    }

    private void Init() {
      if (!inited) {
        StartTimer();
        Main.LogDebug($"[SpawnObjectAnywhere] Orientation target of '{orientationTarget.name}' at '{orientationTarget.transform.position}'. Attempting to get closest valid path finding hex.");
        validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget, orientationTarget.transform.position, $"OrientationTarget.{orientationTarget.name}");
        inited = true;
      }
    }

    public override void Run(RunPayload payload) {
      if (!GetObjectReferences()) return;
      if (HasSpawnerTimedOut()) return;

      SaveSpawnPositions(new List<GameObject>() { target });
      Main.Logger.Log($"[SpawnObjectAnywhere] Attemping for '{target.name}'");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Init();

      if (TotalAttemptCount > TotalAttemptMax) {
        HandleFallback(payload, this.objectKey, this.orientationTargetKey);
        return;
      }

      Vector3 newPosition = GetRandomPositionWithinBounds();
      newPosition = GetClosestValidPathFindingHex(null, newPosition, $"NewSpawnPosition.{target.name}", IsLancePlayerLance(objectKey) ? orientationTarget.transform.position : Vector3.zero, 2);
      if (HasSpawnerTimedOut()) return;

      Main.LogDebug($"[SpawnObjectAnywhere] Attempting selection of random position in bounds. Selected position '{newPosition}'");
      target.transform.position = newPosition;

      if (useOrientationTarget) RotateToTarget(target, orientationTarget);

      if (IsDistanceSetupValid(newPosition)) {
        if (!IsObjectSpawnValid(target, validOrientationTargetPosition)) {
          CheckAttempts();
          Run(payload);
        } else {
          Main.Logger.Log("[SpawnObjectAnywhere] Object spawn complete");
          Vector3 spawnPointPosition = target.transform.position.GetClosestHexLerpedPointOnGrid();
          target.transform.position = spawnPointPosition;
        }
      } else {
        Main.Logger.Log("[SpawnObjectAnywhere] Object spawn is too close to the target. Selecting a new spawn.");
        CheckAttempts();
        Run(payload);
      }
    }


    public bool IsDistanceSetupValid(Vector3 newSpawnPosition) {
      if (distanceCheckType == WithinOrBeyondDistanceType.NONE) return true;

      switch (distanceCheckType) {
        case WithinOrBeyondDistanceType.NONE: return true;
        case WithinOrBeyondDistanceType.MUST_BE_WITHIN: {
          return IsWithinBoundedDistanceOfTarget(newSpawnPosition, validOrientationTargetPosition, distanceCheck);
        }
        case WithinOrBeyondDistanceType.MUST_BE_BEYOND: {
          return IsBeyondBoundedDistanceOfTarget(newSpawnPosition, validOrientationTargetPosition, distanceCheck);
        }
        case WithinOrBeyondDistanceType.BOTH: {
          bool success = IsWithinBoundedDistanceOfTarget(newSpawnPosition, validOrientationTargetPosition, mustBeWithinDistance);
          if (success) success = IsBeyondBoundedDistanceOfTarget(newSpawnPosition, validOrientationTargetPosition, mustBeBeyondDistance);
          return success;
        }
        default: return false;
      }
    }

    private void CheckAttempts() {
      AttemptCount++;
      TotalAttemptCount++;

      if (AttemptCount > AttemptCountMax) {
        AttemptCount = 0;
        Main.LogDebug($"[SpawnObjectAnywhere] Cannot find a suitable object spawn within the boundaries of '{mustBeBeyondDistance}'. Widening search");
        mustBeBeyondDistance -= 50f;
        if (mustBeBeyondDistance <= 10) mustBeBeyondDistance = 10;
      }
    }

    protected override bool GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(objectKey, out target);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (target == null) {
        Main.Logger.LogWarning($"[SpawnObjectAnywhere] Object reference for target '{objectKey}' is null. This will be handled gracefully.");
        return false;
      }

      return true;
    }
  }
}