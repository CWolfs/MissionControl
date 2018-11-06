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
    private bool useMiniumDistance = false;
    private float minimumDistance = 400f;
    private bool clusterUnits = false;

    private int AttemptCountMax { get; set; } = 10;
    private int AttemptCount { get; set; } = 0;
    private int EdgeCheckMax { get; set; } = 10;
    private int EdgeCheckCount { get; set; } = 0;

    private bool inited = false;
    private Vector3 validOrientationTargetPosition;

    public SpawnLanceAtEdgeOfBoundary(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.clusterUnits = clusterUnits;
    }

    public SpawnLanceAtEdgeOfBoundary(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, float minimumDistance, bool clusterUnits = false) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.useMiniumDistance = true;
      this.minimumDistance = minimumDistance;
      this.clusterUnits = clusterUnits;
    }

    private void Init() {
      if (!inited) {
        Main.LogDebug($"[SpawnLanceAtEdgeBoundary] Orientation target of '{orientationTarget.name}' at '{orientationTarget.transform.position}'. Attempting to get closest valid path finding hex.");
        validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget.transform.position, 3);

        // Cluster units to make a tigher spread - makes hitting a successful spawn position generally easier
        if (clusterUnits) {
          ClusterLanceMembers(lance);
          clusterUnits = false;
        }

        inited = true;
      }
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      SaveSpawnPositions(lance);
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] Attemping for '{lance.name}'");
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
      RectEdgePosition xzEdge = usableBounds.CalculateRandomXZEdge(boundary.transform.position, edge);

      Vector3 lancePosition = lance.transform.position.GetClosestHexLerpedPointOnGrid();
      Vector3 newSpawnPosition = new Vector3(xzEdge.Position.x, lancePosition.y, xzEdge.Position.z);
      newSpawnPosition = newSpawnPosition.GetClosestHexLerpedPointOnGrid();
      lance.transform.position = newSpawnPosition;
      
      Main.LogDebug($"[SpawnLanceAtEdgeBoundary] Attempting to spawn lance at point on lerped grid '{newSpawnPosition}'");

      if (useOrientationTarget) RotateToTarget(lance, orientationTarget);

      if (!useMiniumDistance || IsWithinBoundedDistanceOfTarget(newSpawnPosition, validOrientationTargetPosition, minimumDistance)) {
        List<GameObject> invalidLanceSpawns = GetInvalidLanceMemberSpawns(lance, validOrientationTargetPosition);

        if (invalidLanceSpawns.Count > 0) {
          if (AttemptCount > AttemptCountMax) {  // Attempt to spawn on the selected edge. If it's not possible, select another edge
            edge = RectExtensions.RectEdge.ANY;
            if (EdgeCheckCount >= EdgeCheckMax) {
              HandleFallback(payload, this.lanceKey, this.orientationTargetKey);
              return;
            }
          }

          if (invalidLanceSpawns.Count <= 2) {
            Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] Fitting invalid lance member spawns");
            foreach (GameObject invalidSpawn in invalidLanceSpawns) {
              SpawnLanceMember(invalidSpawn);
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
        Main.LogDebug("[SpawnLanceAtEdgeOfBoundary] Spawn is too close to the target. Selecting a new spawn.");
        edge = xzEdge.Edge;
        Run(payload);
      }
    }

    private void SpawnLanceMember(GameObject spawnPoint) {
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] Fitting member '{spawnPoint.name}'");
      Vector3 newSpawnLocation = GetClosestValidPathFindingHex(spawnPoint.transform.position);
      spawnPoint.transform.position = newSpawnLocation;
    }

    private void CheckAttempts() {
      if (AttemptCount > AttemptCountMax) {
        EdgeCheckCount++;
        AttemptCount = 0;
        Main.LogDebug($"[SpawnLanceAtEdgeOfBoundary] Cannot find a suitable lance spawn within the boundaries of {minimumDistance}. Widening search");
        minimumDistance -= 50f;
        if (minimumDistance <= 0f) minimumDistance = 0f;
      }
    }

    protected override void GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(lanceKey, out lance);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (lance == null) {
        Main.Logger.LogError("[SpawnLanceAtEdgeOfBoundary] Object references are null");
      }
    }
  }
}