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
        Main.LogDebug($"[SpawnObjectAnywhere] Orientation target of '{orientationTarget.name}' at '{orientationTarget.transform.position}'. Resolving verified target hex.");
        validOrientationTargetPosition = ResolveVerifiedOrientationTargetPosition(orientationTarget, $"OrientationTarget.{orientationTarget.name}");
        inited = true;
      }
    }

    public override void Run(RunPayload payload) {
      if (!GetObjectReferences()) return;

      SaveSpawnPositions(new List<GameObject>() { target });
      Main.Logger.Log($"[SpawnObjectAnywhere] Attemping for '{target.name}'");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Init();

      if (TotalAttemptCount > TotalAttemptMax) {
        if (TryPlaceObjectAtSafeFallback()) {
          return;
        }

        RestoreSpawnPositions(new List<GameObject>() { target });
        RestorePairedOrientationTarget();
        Main.Logger.LogWarning($"[SpawnObjectAnywhere] Could not find valid spawn for '{target.name}'. Restored original position (and paired target '{orientationTargetKey}' if relocated).");
        return;
      }

      Vector3 newPosition = GetRandomPositionWithinBounds();
      Vector3 pathfindTarget = IsLancePlayerLance(objectKey) ? validOrientationTargetPosition : Vector3.zero;
      if (!TryGetClosestValidPathFindingHex(null, newPosition, $"NewSpawnPosition.{target.name}", pathfindTarget, out newPosition, 2)) {
        Main.LogDebugWarning("[SpawnObjectAnywhere] Could not find valid spawn position. Selecting a new spawn.");
        CheckAttempts();
        Run(payload);
        return;
      }
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


    private void RestorePairedOrientationTarget() {
      if (string.IsNullOrEmpty(orientationTargetKey) || EncounterRules == null) return;
      foreach (LogicBlock logicBlock in EncounterRules.EncounterLogic) {
        if (logicBlock is SpawnLanceLogic spawnLanceLogic && spawnLanceLogic.TargetLanceKey == orientationTargetKey) {
          spawnLanceLogic.RestoreOriginalLanceSpawn();
        }
      }
    }

    private float GetRequiredTargetSeparation() {
      float requiredSeparation = 30f;
      if (target == null) return requiredSeparation;

      RegionGameLogic region = target.GetComponent<RegionGameLogic>();
      if (region != null) {
        float footprintRadius = region.radius;
        RegionPointGameLogic[] regionPoints = region.regionPointList;
        if (regionPoints != null && regionPoints.Length > 0) {
          Vector3 currentCenter = target.transform.position;
          foreach (RegionPointGameLogic rp in regionPoints) {
            if (rp != null) {
              float dist = currentCenter.DistanceFlat(rp.transform.position);
              if (dist > footprintRadius) footprintRadius = dist;
            }
          }
        }
        requiredSeparation = Mathf.Max(requiredSeparation, footprintRadius + 24f);
      }

      return requiredSeparation;
    }

    private bool IsSeparatedFromOrientationTarget(Vector3 position, float minDistance) {
      if (!useOrientationTarget) return true;
      if (position.DistanceFlat(validOrientationTargetPosition) <= minDistance) return false;
      if (orientationTarget != null) {
        if (position.DistanceFlat(orientationTarget.transform.position) <= minDistance) return false;
        List<GameObject> targetSpawns = orientationTarget.FindAllContains("SpawnPoint");
        foreach (GameObject targetSpawn in targetSpawns) {
          if (position.DistanceFlat(targetSpawn.transform.position) <= minDistance) return false;
        }
      }
      return true;
    }

    private bool TryPlaceObjectAtSafeFallback() {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      float requiredSeparation = GetRequiredTargetSeparation();

      // 1. Try relaxed-distance random placements in bounds that remain reachable and clear of the target footprint
      for (int attempt = 0; attempt < 5; attempt++) {
        Vector3 candidate = GetRandomPositionWithinBounds();
        if (!TryGetClosestValidPathFindingHex(null, candidate, $"FallbackObject.{target.name}.{attempt}", validOrientationTargetPosition, out candidate, 2)) {
          continue;
        }

        target.transform.position = candidate;
        if (!IsObjectSpawnValid(target, validOrientationTargetPosition)) continue;

        Vector3 finalPos = target.transform.position.GetClosestHexLerpedPointOnGrid();
        if (!IsSeparatedFromOrientationTarget(finalPos, requiredSeparation)) continue;

        target.transform.position = finalPos;
        if (useOrientationTarget && orientationTarget != null) RotateToTarget(target, orientationTarget);
        Main.Logger.LogWarning($"[SpawnObjectAnywhere] Placed '{target.name}' at relaxed-distance reachable fallback '{target.transform.position}'.");
        return true;
      }

      // 2. Check whether the original vanilla position is still valid and clear of the target footprint after snapping
      RestoreSpawnPositions(new List<GameObject>() { target });
      if (IsObjectSpawnValid(target, validOrientationTargetPosition)) {
        Vector3 finalRestoredPos = target.transform.position.GetClosestHexLerpedPointOnGrid();
        if (IsSeparatedFromOrientationTarget(finalRestoredPos, requiredSeparation)) {
          target.transform.position = finalRestoredPos;
          Main.Logger.LogWarning($"[SpawnObjectAnywhere] Restored '{target.name}' to verified original position '{target.transform.position}'.");
          return true;
        }
      }

      // 3. Bounded nearest-first ring search around the orientation target for a reachable dry hex outside the region footprint
      int ringRadius = Mathf.Clamp(Mathf.CeilToInt(requiredSeparation / combatState.HexGrid.HexWidth) + 8, 15, 25);
      List<Vector3> ringHexes = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(validOrientationTargetPosition, ringRadius);
      ringHexes.Sort((a, b) => (a - validOrientationTargetPosition).sqrMagnitude.CompareTo((b - validOrientationTargetPosition).sqrMagnitude));
      int budget = 40;
      foreach (Vector3 rawHex in ringHexes) {
        if (budget <= 0) break;
        Vector3 hex = rawHex.GetClosestHexLerpedPointOnGrid();
        if (!IsSeparatedFromOrientationTarget(hex, requiredSeparation)) continue;
        if (!encounterLayerData.IsInEncounterBounds(hex)) continue;
        if (!PathFinderManager.Instance.IsCellValidDrySpawn(combatState, hex)) continue;

        budget--;
        if (!PathFinderManager.Instance.IsSpawnValid(target, hex, validOrientationTargetPosition, UnitType.Mech, target.name)) continue;

        target.transform.position = hex;
        if (useOrientationTarget && orientationTarget != null) RotateToTarget(target, orientationTarget);
        Main.Logger.LogWarning($"[SpawnObjectAnywhere] Placed '{target.name}' at reachable ring fallback '{target.transform.position}'.");
        return true;
      }

      return false;
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