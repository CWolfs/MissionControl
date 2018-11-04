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
    private bool useMiniumDistance = false;
    private float minimumDistance = 400f;
    private bool clusterUnits = false;

    private int AttemptCountMax { get; set; } = 5;
    private int AttemptCount { get; set; } = 0;
    private int TotalAttemptMax { get; set; } = 5;
    private int TotalAttemptCount { get; set; } = 0;
    private Vector3 vanillaPosition;

    public SpawnLanceAnywhere(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
      this.clusterUnits = clusterUnits;
    }

    public SpawnLanceAnywhere(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, float minimumDistance, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.useMiniumDistance = true;
      this.minimumDistance = minimumDistance;
      this.clusterUnits = clusterUnits;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      SaveSpawnPositions(lance);
      Main.Logger.Log($"[SpawnLanceAnywhere] Attemping for '{lance.name}'");
      vanillaPosition = lance.transform.position;
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      // Cluster units to make a tigher spread - makes hitting a successful spawn position generally easier
      if (clusterUnits) {
        ClusterLanceMembers(lance);
        clusterUnits = false;
      }

      if (TotalAttemptCount >= TotalAttemptMax) {
        HandleFallback(payload, this.lanceKey, this.orientationTargetKey);
        return;
      }

      Vector3 newPosition = GetRandomPositionWithinBounds();
      Main.LogDebug($"[SpawnLanceAnywhere] Attempting selection of random position in bounds. Selected position '{newPosition}'");
      lance.transform.position = newPosition;

      Vector3 validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget.transform.position);

      if (useOrientationTarget) RotateToTarget(lance, orientationTarget);

      if (!useMiniumDistance || IsWithinBoundedDistanceOfTarget(newPosition, validOrientationTargetPosition, minimumDistance)) {
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

    private void CheckAttempts() {
      AttemptCount++;
      TotalAttemptCount++;

      if (AttemptCount > AttemptCountMax) {
        AttemptCount = 0;
        Main.LogDebug($"[SpawnLanceAnywhere] Cannot find a suitable lance spawn within the boundaries of {minimumDistance}. Widening search");
        minimumDistance -= 50f;
        if (minimumDistance <= 10) minimumDistance = 10;
      }
    }

    protected override void GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(lanceKey, out lance);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (lance == null) {
        Main.Logger.LogError("[SpawnLanceAroundTarget] Object references are null");
      }
    }
  }
}