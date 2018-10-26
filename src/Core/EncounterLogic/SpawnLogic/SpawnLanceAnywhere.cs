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

    private int AttemptCountMax { get; set; } = 5;
    private int AttemptCount { get; set; } = 0;
    private Vector3 vanillaPosition;

    public SpawnLanceAnywhere(EncounterRules encounterRules, string lanceKey, string orientationTargetKey) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
    }

    public SpawnLanceAnywhere(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, float minimumDistance) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.useMiniumDistance = true;
      this.minimumDistance = minimumDistance;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[SpawnLanceAnywhere] Attemping for '{lance.name}'");
      vanillaPosition = lance.transform.position;
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Vector3 newPosition = GetRandomPositionWithinBounds();
      Main.LogDebug($"[SpawnLanceAnywhere] Attempting selection of random position in bounds. Selected position '{newPosition}'");
      lance.transform.position = newPosition;

      Vector3 validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget.transform.position);

      if (useOrientationTarget) RotateLanceMembersToTarget(lance, orientationTarget);

      if (!useMiniumDistance || IsWithinBoundedDistanceOfTarget(newPosition, validOrientationTargetPosition, minimumDistance)) {
        if (!AreLanceMemberSpawnsValid(lance, validOrientationTargetPosition)) {
          AttemptCount++;
          Run(payload);
        } else {
          Main.Logger.Log("[SpawnLanceAnywhere] Lance spawn complete");
          CorrectLanceMemberSpawns(lance);
        }
      } else {
        Main.Logger.Log("[SpawnLanceAnywhere] Spawn is too close to the target. Selecting a new spawn.");
        AttemptCount++;

        if (AttemptCount > AttemptCountMax) {
          AttemptCount = 0;
          minimumDistance -= 50f;
          if (minimumDistance <= 0) {
            if (vanillaPosition == Vector3.zero) {
              Main.LogDebug($"[SpawnLanceAnywhere] Cannot find valid spawn. Not spawning.");
            } else {
              lance.transform.position = vanillaPosition;
              Main.LogDebug($"[SpawnLanceAnywhere] Cannot find valid spawn. Spawning at vanilla location for the encounter");
            }
          }
        } else { 
          Run(payload);
        }
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