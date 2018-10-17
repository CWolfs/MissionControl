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
    private string knownValidLocationKey = "";

    private GameObject lance;
    private GameObject orientationTarget;
    private GameObject knownValidLocation;
    private float minDistanceFromTarget = 50f;
    private float maxDistanceFromTarget = 150f;
    private LookDirection lookDirection = LookDirection.TOWARDS_TARGET;

    private int AttemptCountMax { get; set; } = 10;
    private int AttemptCount { get; set; } = 0;

    public SpawnLanceAroundTarget(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, LookDirection lookDirection) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
      this.lookDirection = lookDirection;
    }

    public SpawnLanceAroundTarget(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, LookDirection lookDirection, float minDistance, float maxDistance) :
      this(encounterRules, lanceKey, orientationTargetKey, orientationTargetKey, lookDirection, minDistance, maxDistance) {}

    public SpawnLanceAroundTarget(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, string knownValidLocationKey, LookDirection lookDirection, float minDistance, float maxDistance) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
      this.knownValidLocationKey = knownValidLocationKey;
      this.minDistanceFromTarget = minDistance;
      this.maxDistanceFromTarget = maxDistance;
      this.lookDirection = lookDirection;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[SpawnLanceAroundTarget] For {lance.name}");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      MissionControl encounterManager = MissionControl.Instance;

      Vector3 newSpawnPosition = GetRandomPositionFromTarget(orientationTarget, minDistanceFromTarget, maxDistanceFromTarget);

      if (encounterManager.EncounterLayerData.IsInEncounterBounds(newSpawnPosition)) {
        lance.transform.position = newSpawnPosition;

        if (lookDirection == LookDirection.TOWARDS_TARGET) {
          RotateToTarget(lance, orientationTarget);
        } else {
          RotateAwayFromTarget(lance, orientationTarget);
        }

        if (!AreLanceMemberSpawnsValid(lance, knownValidLocation)) {
          CheckAttempts();
          Run(payload);
        } else {
          Main.Logger.Log("[SpawnLanceAroundTarget] Lance spawn complete");
        }
      } else {
        Main.Logger.LogWarning("[SpawnLanceAroundTarget] Selected lance spawn point is outside of the boundary. Select a new lance spawn point.");
        CheckAttempts();
        Run(payload);
      }
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
      this.EncounterRules.ObjectLookup.TryGetValue(knownValidLocationKey, out knownValidLocation);

      if (lance == null || orientationTarget == null) {
        Main.Logger.LogError("[SpawnLanceAroundTarget] Object references are null");
      }
    }
  }
}