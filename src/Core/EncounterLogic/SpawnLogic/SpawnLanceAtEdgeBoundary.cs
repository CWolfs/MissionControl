using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class SpawnLanceAtEdgeOfBoundary : SpawnLanceLogic {
    private string lanceKey = "";
    private string orientationTargetKey = "";

    private GameObject lance;
    private bool useOrientationTarget = false;
    private GameObject orientationTarget;
    private RectExtensions.RectEdge edge = RectExtensions.RectEdge.ANY;

    private WithinOrBeyondDistanceType distanceCheckType = WithinOrBeyondDistanceType.NONE;
    private float distanceCheck = 400f;
    private float mustBeWithinDistance = 400f;
    private float mustBeBeyondDistance = 400f;

    private bool clusterUnits = false;

    private int AttemptCountMax { get; set; } = 3;
    private int AttemptCount { get; set; } = 0;
    private int EdgeCheckMax { get; set; } = 3;
    private int EdgeCheckCount { get; set; } = 0;

    private bool inited = false;
    private Vector3 validOrientationTargetPosition;

    public SpawnLanceAtEdgeOfBoundary(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.clusterUnits = clusterUnits;
    }

    public SpawnLanceAtEdgeOfBoundary(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, float mustBeBeyondDistance, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.distanceCheckType = WithinOrBeyondDistanceType.MUST_BE_BEYOND;
      this.distanceCheck = mustBeBeyondDistance;
      this.clusterUnits = clusterUnits;
    }

    public SpawnLanceAtEdgeOfBoundary(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, WithinOrBeyondDistanceType distanceCheckType, float checkDistance, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.distanceCheckType = distanceCheckType;
      this.distanceCheck = checkDistance;
      this.clusterUnits = clusterUnits;
    }

    /*
    * Beyond = Must be beyond X distance
    * Within = Must be within Y distance
    */
    public SpawnLanceAtEdgeOfBoundary(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, float mustBeBeyondDistance, float mustBeWithinDistance, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.distanceCheckType = WithinOrBeyondDistanceType.BOTH;
      this.mustBeBeyondDistance = mustBeBeyondDistance;
      this.mustBeWithinDistance = mustBeWithinDistance;
      this.distanceCheck = mustBeBeyondDistance;
      this.clusterUnits = clusterUnits;
    }

    private void Init(bool forced = false) {
      if (!inited || forced) {
        Main.LogDebug($"[SpawnLanceAtEdgeBoundary] Forcing Re-init");
        Main.LogDebug($"[SpawnLanceAtEdgeBoundary] Orientation target of '{orientationTarget.name}' at '{orientationTarget.transform.position}'. Attempting to get closest valid path finding hex.");
        validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget, orientationTarget.transform.position, $"OrientationTarget.{orientationTarget.name}");

        // Cluster units to make a tigher spread - makes hitting a successful spawn position generally easier
        // TODO: Investigate if this causes bad grid placement since there's no 'IsSpawnValid' check in this call stack
        if (clusterUnits) {
          Main.LogDebug($"[SpawnLanceAtEdgeBoundary] Clustering lance '{lance.name}'");
          ClusterLanceMembers(lance);
          clusterUnits = false;
          Main.LogDebug($"[SpawnLanceAtEdgeBoundary] Finished clustering lance '{lance.name}'");
        }

        inited = true;
      }
    }

    public override void Run(RunPayload payload) {
      if (!GetObjectReferences()) return;

      SaveSpawnPositions(lance);
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] Attemping for '{lance.name}'. Attempt: '{AttemptCount}/{AttemptCountMax}' and Edge Check: '{EdgeCheckCount}/{EdgeCheckMax}'");
      AttemptCount++;

      Init();

      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      MissionControl EncounterManager = MissionControl.Instance;
      GameObject chunkBoundaryRect = EncounterManager.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();
      Rect usableBounds = boundaryRec.GenerateUsableBoundary();

      if (AttemptCount > AttemptCountMax) {  // Attempt to spawn on the selected edge. If it's not possible, select another edge
        edge = RectExtensions.RectEdge.ANY;
        if (EdgeCheckCount >= EdgeCheckMax) {
          HandleFallback(payload, this.lanceKey, this.orientationTargetKey);
          return;
        }
      }

      RectEdgePosition xzEdge = usableBounds.CalculateRandomXZEdge(boundary.transform.position, edge);

      Vector3 lancePosition = lance.transform.position.GetClosestHexLerpedPointOnGrid();
      Vector3 newSpawnPosition = new Vector3(xzEdge.Position.x, lancePosition.y, xzEdge.Position.z);
      newSpawnPosition = GetClosestValidPathFindingHex(lance, newSpawnPosition, $"NewSpawnPosition.{lance.name}", IsLancePlayerLance(lanceKey) ? orientationTarget.transform.position : Vector3.zero, 2);
      lance.transform.position = newSpawnPosition;

      Main.LogDebug($"[SpawnLanceAtEdgeBoundary] Attempting to spawn lance at point on lerped grid '{newSpawnPosition}'");

      if (useOrientationTarget) RotateToTarget(lance, orientationTarget);

      if (IsDistanceSetupValid(newSpawnPosition)) {
        List<GameObject> invalidLanceSpawns = GetInvalidLanceMemberSpawns(lance, validOrientationTargetPosition);

        if (invalidLanceSpawns.Count > 0) {
          if (invalidLanceSpawns.Count <= 2) {
            Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] Fitting invalid lance member spawns");
            foreach (GameObject invalidSpawn in invalidLanceSpawns) {
              SpawnLanceMember(invalidSpawn, lance.transform.position);
            }

            Main.Logger.Log("[SpawnLanceAtEdgeOfBoundary] Lance spawn complete");
          } else {
            CheckAttempts();
            Run(payload);
          }
        } else {
          CorrectLanceMemberSpawns(lance);
          Main.Logger.Log("[SpawnLanceAtEdgeOfBoundary] Lance spawn complete");
        }
      } else {
        CheckAttempts();
        Main.LogDebug("[SpawnLanceAtEdgeOfBoundary] Spawn is too close or too far to/from the target. Selecting a new spawn.");
        edge = xzEdge.Edge;
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

    private void SpawnLanceMember(GameObject spawnPoint, Vector3 anchorPoint) {
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary.SpawnLanceMember] Fitting member '{spawnPoint.name}' at anchor point '{anchorPoint}'");
      Vector3 newSpawnLocation = GetClosestValidPathFindingHex(spawnPoint, anchorPoint, $"SpawnLanceMember.{spawnPoint.name}", IsLancePlayerLance(lanceKey) ? orientationTarget.transform.position : Vector3.zero, 2);

      if (!newSpawnLocation.IsTooCloseToAnotherSpawn(spawnPoint)) {
        spawnPoint.transform.position = newSpawnLocation;
      } else {
        SpawnLanceMember(spawnPoint, anchorPoint);
      }
    }

    private void CheckAttempts() {
      if (AttemptCount > AttemptCountMax) {
        EdgeCheckCount++;
        AttemptCount = 0;

        if (distanceCheckType == WithinOrBeyondDistanceType.NONE || distanceCheckType == WithinOrBeyondDistanceType.MUST_BE_BEYOND) {
          Main.LogDebug($"[SpawnLanceAtEdgeOfBoundary] Cannot find a suitable lance spawn beyond the boundaries of {distanceCheck}. Widening search");
          distanceCheck -= 50f;
          if (distanceCheck <= 0f) distanceCheck = 0f;
        } else {
          Main.LogDebug($"[SpawnLanceAtEdgeOfBoundary] Cannot find a suitable lance spawn within the boundaries of {distanceCheck}. Widening search");
          distanceCheck += 50f;
        }
      }
    }

    protected override bool GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(lanceKey, out lance);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (lance == null) {
        Main.Logger.LogWarning($"[SpawnLanceAtEdgeOfBoundary] Object reference for target '{lanceKey}' is null. This will be handled gracefully.");
        return false;
      }

      return true;
    }
  }
}