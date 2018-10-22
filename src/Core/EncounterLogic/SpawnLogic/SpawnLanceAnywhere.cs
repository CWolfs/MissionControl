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

    private int AttemptCountMax { get; set; } = 10;
    private int AttemptCount { get; set; } = 0;

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
      Main.Logger.Log($"[SpawnLanceAnywhere] For {lance.name}");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Vector3 newPosition = GetRandomPositionWithinBounds();
      Main.Logger.Log($"[SpawnLanceAnywhere] Attempting selection of random position in bounds. Selected position '{newPosition}'");
      lance.transform.position = newPosition;

      Vector3 validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget.transform.position);

      if (useOrientationTarget) RotateToTarget(lance, orientationTarget);

      if (!useMiniumDistance || IsWithinBoundedDistanceOfTarget(newPosition, validOrientationTargetPosition, minimumDistance)) {
        if (!AreLanceMemberSpawnsValid(lance, validOrientationTargetPosition)) {
          Run(payload);
        } else {
          Main.Logger.Log("[SpawnLanceAnywhere] Lance spawn complete");
          CorrectLanceMemberSpawns(lance);
        }
      } else {
        Main.Logger.Log("[SpawnLanceAnywhere] Spawn is too close to the target. Selecting a new spawn.");
        Run(payload);
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