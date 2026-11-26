using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class SpawnLanceAroundTarget : SpawnLanceLogic {
    private string lanceKey = "";
    private string orientationTargetKey = "";

    private GameObject lance;
    private GameObject orientationTarget;
    private float minDistanceFromTarget = 50f;
    private float maxDistanceFromTarget = 150f;
    private LookDirection lookDirection = LookDirection.TOWARDS_TARGET;
    private bool fitLanceMembers = false;

    private int AttemptCountMax { get; set; } = 3;
    private int AttemptCount { get; set; } = 0;
    private int TotalAttemptMax { get; set; } = 3;
    private int TotalAttemptCount { get; set; } = 0;

    private bool inited = false;
    private Vector3 validOrientationTargetPosition;

    public SpawnLanceAroundTarget(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, LookDirection lookDirection) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
      this.lookDirection = lookDirection;
    }

    public SpawnLanceAroundTarget(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, LookDirection lookDirection, float minDistance, float maxDistance, bool fitLanceMembers) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
      this.minDistanceFromTarget = minDistance;
      this.maxDistanceFromTarget = maxDistance;
      this.lookDirection = lookDirection;
      this.fitLanceMembers = fitLanceMembers;
    }

    public override string TargetLanceKey => lanceKey;

    private void Init() {
      if (!inited) {
        Main.LogDebug($"[SpawnLanceAroundTarget] Orientation target of '{orientationTarget.name}' at '{orientationTarget.transform.position}'. Resolving verified target hex.");
        validOrientationTargetPosition = ResolveVerifiedOrientationTargetPosition(orientationTarget, $"OrientationTarget.{orientationTarget.name}");
        inited = true;
      }
    }

    public override void Run(RunPayload payload) {
      if (!GetObjectReferences()) return;

      SaveSpawnPositions(lance);
      SaveAttemptFormation(lance);
      Main.Logger.Log($"[SpawnLanceAroundTarget] Attemping for '{lance.name}'");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      MissionControl encounterManager = MissionControl.Instance;

      Init();

      if (TotalAttemptCount >= TotalAttemptMax) {
        HandleFallback(payload, this.lanceKey, this.orientationTargetKey);
        return;
      }

      Vector3 newSpawnPosition = GetRandomPositionFromTarget(validOrientationTargetPosition, minDistanceFromTarget, maxDistanceFromTarget);
      Vector3 pathfindTarget = IsLancePlayerLance(lanceKey) ? validOrientationTargetPosition : Vector3.zero;
      bool foundSpawn = TryGetClosestValidPathFindingHex(lance, newSpawnPosition, $"NewRandomSpawnPositionFromOrientationTarget.{orientationTarget.name}", pathfindTarget, out newSpawnPosition, 2);

      if (foundSpawn && encounterManager.EncounterLayerData.IsInEncounterBounds(newSpawnPosition)) {
        lance.transform.position = newSpawnPosition;

        if (lookDirection == LookDirection.TOWARDS_TARGET) {
          RotateToTarget(lance, orientationTarget);
        } else {
          RotateAwayFromTarget(lance, orientationTarget);
        }

        List<GameObject> invalidLanceSpawns = GetInvalidLanceMemberSpawns(lance, validOrientationTargetPosition);

        if (invalidLanceSpawns.Count > 0) {
          if (fitLanceMembers && invalidLanceSpawns.Count <= 2) {
            Main.Logger.Log($"[SpawnLanceAroundTarget] Fitting invalid lance member spawns");
            bool allMembersFitted = true;
            foreach (GameObject invalidSpawn in invalidLanceSpawns) {
              if (!SpawnLanceMember(invalidSpawn, newSpawnPosition)) {
                allMembersFitted = false;
                break;
              }
            }

            if (allMembersFitted) {
              Main.Logger.Log("[SpawnLanceAroundTarget] Lance spawn complete");
            } else {
              Main.LogDebugWarning("[SpawnLanceAroundTarget] Could not fit all invalid lance members safely. Selecting a new lance spawn.");
              RestoreAttemptFormation(lance);
              CheckAttempts();
              Run(payload);
            }
          } else {
            RestoreAttemptFormation(lance);
            CheckAttempts();
            Run(payload);
          }
        } else {
          Main.Logger.Log("[SpawnLanceAroundTarget] Lance spawn complete");
        }
      } else {
        Main.LogDebugWarning("[SpawnLanceAroundTarget] Selected lance spawn point is invalid or outside of the boundary. Select a new lance spawn point.");
        CheckAttempts();
        Run(payload);
      }
    }

    private bool SpawnLanceMember(GameObject spawnPoint, Vector3 anchorPoint) {
      Main.Logger.Log($"[SpawnLanceAroundTarget] Fitting member '{spawnPoint.name}' around anchor point '{anchorPoint}'");
      Vector3 pathfindTarget = IsLancePlayerLance(lanceKey) ? validOrientationTargetPosition : Vector3.zero;
      if (!TryGetClosestValidPathFindingHex(spawnPoint, anchorPoint, $"SpawnLanceMember.{spawnPoint.name}", pathfindTarget, out Vector3 newSpawnLocation, 2)) {
        Main.LogDebugWarning($"[SpawnLanceAroundTarget] Could not find valid pathfinding hex for '{spawnPoint.name}'.");
        return false;
      }

      if (!MissionControl.Instance.EncounterLayerData.IsInEncounterBounds(newSpawnLocation)
          || newSpawnLocation.IsTooCloseToAnotherSpawn(spawnPoint)
          || !PathFinderManager.Instance.IsSpawnValid(spawnPoint, newSpawnLocation, validOrientationTargetPosition, UnitType.Mech, spawnPoint.name)) {
        Main.LogDebugWarning($"[SpawnLanceAroundTarget] Fitted spawn '{newSpawnLocation}' for '{spawnPoint.name}' failed validation.");
        return false;
      }

      spawnPoint.transform.position = newSpawnLocation;
      return true;
    }

    private void CheckAttempts() {
      AttemptCount++;
      TotalAttemptCount++;

      if (AttemptCount > AttemptCountMax) {
        AttemptCount = 0;
        Main.LogDebug($"[SpawnLanceAroundTarget] Cannot find a suitable lance spawn within the boundaries of {minDistanceFromTarget} and {maxDistanceFromTarget}. Widening search");
        minDistanceFromTarget -= 10;
        if (minDistanceFromTarget <= 10) minDistanceFromTarget = 10;
        maxDistanceFromTarget += 25;
      }
    }

    protected override bool GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(lanceKey, out lance);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (lance == null) {
        Main.Logger.LogWarning($"[SpawnLanceAroundTarget] Object reference for target '{lanceKey}' is null. This will be handled gracefully.");
        return false;
      }

      if (orientationTarget == null) {
        Main.Logger.LogWarning($"[SpawnLanceAroundTarget] Object reference for orientation target '{orientationTargetKey}' is null. This will be handled gracefully.");
        return false;
      }

      return true;
    }
  }
}