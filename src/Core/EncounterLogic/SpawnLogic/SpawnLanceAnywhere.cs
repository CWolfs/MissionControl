using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class SpawnLanceAnywhere : SpawnLanceLogic {
    private string lanceKey = "";
    private bool useOrientationTarget = false;
    private string orientationTargetKey = "";
    private GameObject lance;
    private GameObject orientationTarget;

    private WithinOrBeyondDistanceType distanceCheckType = WithinOrBeyondDistanceType.NONE;
    private float distanceCheck = 400f;
    private float mustBeWithinDistance = 400f;
    private float mustBeBeyondDistance = 400f;

    private bool clusterUnits = false;

    private int AttemptCountMax { get; set; } = 5;
    private int AttemptCount { get; set; } = 0;
    private int TotalAttemptMax { get; set; } = 5;
    private int TotalAttemptCount { get; set; } = 0;

    private bool inited = false;
    private Vector3 validOrientationTargetPosition;

    public SpawnLanceAnywhere(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
      this.clusterUnits = clusterUnits;
    }

    public SpawnLanceAnywhere(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, float mustBeBeyondDistance, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.distanceCheckType = WithinOrBeyondDistanceType.MUST_BE_BEYOND;
      this.distanceCheck = mustBeBeyondDistance;
      this.clusterUnits = clusterUnits;
    }

    public SpawnLanceAnywhere(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, float mustBeBeyondDistance, float mustBeWithinDistance, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.distanceCheckType = WithinOrBeyondDistanceType.BOTH;
      this.mustBeBeyondDistance = mustBeBeyondDistance;
      this.mustBeWithinDistance = mustBeWithinDistance;
      this.distanceCheck = mustBeBeyondDistance;
      this.clusterUnits = clusterUnits;
    }

    private void Init() {
      if (!inited) {
        Main.LogDebug($"[SpawnLanceAnywhere] Orientation target of '{orientationTarget.name}' at '{orientationTarget.transform.position}'. Attempting to get closest valid path finding hex.");
        validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget, orientationTarget.transform.position, $"OrientationTarget.{orientationTarget.name}");
        inited = true;
      }
    }

    public override void Run(RunPayload payload) {
      if (!GetObjectReferences()) return;
      if (HasSpawnerTimedOut()) return;

      SaveSpawnPositions(lance);
      Main.Logger.Log($"[SpawnLanceAnywhere] Attemping for '{lance.name}'");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Init();

      // Cluster units to make a tigher spread - makes hitting a successful spawn position generally easier
      if (clusterUnits) {
        Main.LogDebug($"[SpawnLanceAnywhere] Clustering lance '{lance.name}'");
        ClusterLanceMembers(lance);
        clusterUnits = false;
        Main.LogDebug($"[SpawnLanceAnywhere] Finished clustering lance '{lance.name}'");
      }

      if (TotalAttemptCount > TotalAttemptMax) {
        HandleFallback(payload, this.lanceKey, this.orientationTargetKey);
        return;
      }

      Vector3 newPosition = GetRandomPositionWithinBounds();
      newPosition = GetClosestValidPathFindingHex(null, newPosition, $"NewSpawnPosition.{lance.name}", IsLancePlayerLance(lanceKey) ? orientationTarget.transform.position : Vector3.zero, 2);
      if (HasSpawnerTimedOut()) return;
      Main.LogDebug($"[SpawnLanceAnywhere] Attempting selection of random position in bounds. Selected position '{newPosition}'");
      lance.transform.position = newPosition;

      if (useOrientationTarget) RotateToTarget(lance, orientationTarget);

      if (IsDistanceSetupValid(newPosition)) {
        if (!AreLanceMemberSpawnsValid(lance, validOrientationTargetPosition)) {
          CheckAttempts();
          Run(payload);
        } else {
          Main.Logger.Log("[SpawnLanceAnywhere] Lance spawn complete");
          CorrectLanceMemberSpawns(lance);
        }
      } else {
        Main.Logger.Log("[SpawnLanceAnywhere] Spawn is too close to the target. Selecting a new spawn.");
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
        Main.LogDebug($"[SpawnLanceAnywhere] Cannot find a suitable lance spawn within the boundaries of {mustBeBeyondDistance}. Widening search");
        mustBeBeyondDistance -= 50f;
        if (mustBeBeyondDistance <= 10) mustBeBeyondDistance = 10;
      }
    }

    protected override bool GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(lanceKey, out lance);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (lance == null) {
        Main.Logger.LogWarning($"[SpawnLanceAnywhere] Object reference for target '{lanceKey}' is null. This will be handled gracefully.");
        return false;
      }

      return true;
    }
  }
}