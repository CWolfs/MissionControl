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

    /* // TODO: Reactivate when you have a test for centre of map for pathfinding validity
    public SpawnLanceAnywhere(EncounterRules encounterRules, string lanceKey) : base(encounterRules) {
      this.lanceKey = lanceKey;
    }
    */

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
      MissionControl EncounterManager = MissionControl.Instance;
      GameObject chunkBoundaryRect = EncounterManager.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();
      Rect usableBounds = boundaryRec.GenerateUsableBoundary();
      Vector3 newPosition = usableBounds.CalculateRandomPosition(boundaryLogic.Position);

      Vector3 lancePosition = lance.transform.position;
      Vector3 newSpawnPosition = lancePosition.GetClosestHexLerpedPointOnGrid();
      lance.transform.position = newSpawnPosition;

      Vector3 validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget.transform.position);

      if (useOrientationTarget) RotateToTarget(lance, orientationTarget);

      if (!useMiniumDistance || IsWithinBoundedDistanceOfTarget(lance.transform.position, validOrientationTargetPosition, minimumDistance)) {
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