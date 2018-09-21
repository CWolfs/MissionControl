using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Rules;
using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class SpawnLanceAroundTarget : SpawnLanceLogic {
    private string lanceKey;
    private string orientationTargetKey;

    private GameObject lance;
    private GameObject orientationTarget;
    private float minDistanceFromTarget = 50f;
    private float maxDistanceFromTarget = 150f;
    private LookDirection lookDirection = LookDirection.TOWARDS_TARGET;

    public SpawnLanceAroundTarget(EncounterRule encounterRule, string lanceKey, string orientationTargetKey, LookDirection lookDirection) : base(encounterRule) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
      this.lookDirection = lookDirection;
    }

    public SpawnLanceAroundTarget(EncounterRule encounterRule, string lanceKey, string orientationTargetKey, LookDirection lookDirection, float minDistance, float maxDistance) : base(encounterRule) {
      this.lanceKey = lanceKey;
      this.minDistanceFromTarget = minDistance;
      this.maxDistanceFromTarget = maxDistance;
      this.lookDirection = lookDirection;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[SpawnLanceAroundTarget] For {lance.name}");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      SpawnManager spawnManager = SpawnManager.GetInstance();

      Vector3 lancePosition = lance.transform.position;
      Vector3 newSpawnPosition = GetRandomPositionFromTarget(lance, minDistanceFromTarget, maxDistanceFromTarget);
      newSpawnPosition.y = combatState.MapMetaData.GetLerpedHeightAt(newSpawnPosition);
      lance.transform.position = newSpawnPosition;

      if (lookDirection == LookDirection.TOWARDS_TARGET) {
        RotateToTarget(lance, orientationTarget);
      } else {
        RotateAwayFromTarget(lance, orientationTarget);
      }

      if (!AreLanceMemberSpawnsValid(lance, orientationTarget)) {
        Run(payload);
      } else {
        Main.Logger.Log("[SpawnLanceAroundTarget] Lance spawn complete");
      }
    }

    protected override void GetObjectReferences() {
      this.EncounterRule.ObjectLookup.TryGetValue(lanceKey, out lance);
      this.EncounterRule.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (lance == null || orientationTarget == null) {
        Main.Logger.LogError("[SpawnLanceAroundTarget] Object references are null");
      }
    }
  }
}