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

    private int AttemptCountMax { get; set; } = 10;
    private int AttemptCount { get; set; } = 0;

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

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[SpawnLanceAroundTarget] For {lance.name}");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      MissionControl encounterManager = MissionControl.Instance;

      Vector3 validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget.transform.position);
      Vector3 newSpawnPosition = GetRandomPositionFromTarget(validOrientationTargetPosition, minDistanceFromTarget, maxDistanceFromTarget);

      if (encounterManager.EncounterLayerData.IsInEncounterBounds(newSpawnPosition)) {
        lance.transform.position = newSpawnPosition;

        if (lookDirection == LookDirection.TOWARDS_TARGET) {
          RotateToTarget(lance, orientationTarget);
        } else {
          RotateAwayFromTarget(lance, orientationTarget);
        }

        List<GameObject> invalidLanceSpawns = GetInvalidLanceMemberSpawns(lance, validOrientationTargetPosition);

        if (invalidLanceSpawns.Count > 0) {
          if (fitLanceMembers & invalidLanceSpawns.Count <= 2) {
            Main.Logger.Log($"[SpawnLanceAroundTarget] Fitting invalid lance member spawns");
            foreach (GameObject invalidSpawn in invalidLanceSpawns) {
              SpawnLanceMember(invalidSpawn);
            }
          } else {
            CheckAttempts();
            Run(payload);
          }
        } else {
          Main.Logger.Log("[SpawnLanceAroundTarget] Lance spawn complete");
        }
      } else {
        Main.Logger.LogWarning("[SpawnLanceAroundTarget] Selected lance spawn point is outside of the boundary. Select a new lance spawn point.");
        CheckAttempts();
        Run(payload);
      }
    }

    private void SpawnLanceMember(GameObject spawnPoint) {
      Main.Logger.Log($"[SpawnLanceAroundTarget] Fitting member '{spawnPoint.name}'");
      Vector3 newSpawnLocation = GetClosestValidPathFindingHex(spawnPoint.transform.position);
      spawnPoint.transform.position = newSpawnLocation;
    }

    private void CheckAttempts() {
      AttemptCount++;

      if (AttemptCount > AttemptCountMax) {
        AttemptCount = 0;
        Main.Logger.Log($"[SpawnLanceAroundTarget] Cannot find a suitable lance spawn within the boundaries of {minDistanceFromTarget} and {maxDistanceFromTarget}. Widening search");
        minDistanceFromTarget -= 10;
        if (minDistanceFromTarget <= 10) minDistanceFromTarget = 10;
        maxDistanceFromTarget += 25;     
      }
    }

    protected override void GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(lanceKey, out lance);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (lance == null || orientationTarget == null) {
        Main.Logger.LogError("[SpawnLanceAroundTarget] Object references are null");
      }
    }
  }
}