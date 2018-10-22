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

    private Vector3 vanillaPosition;

    private int AttemptCountMax { get; set; } = 10;
    private int AttemptCount { get; set; } = 0;

    public SpawnLanceAtEdgeOfBoundary(EncounterRules encounterRules, string lanceKey, string orientationTargetKey) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
    }

    public SpawnLanceAtEdgeOfBoundary(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, float minimumDistance) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.useOrientationTarget = true;
      this.orientationTargetKey = orientationTargetKey;
      this.useMiniumDistance = true;
      this.minimumDistance = minimumDistance;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] For {lance.name}");
      vanillaPosition = lance.transform.position;

      AttemptCount++;
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

      Vector3 validOrientationTargetPosition = GetClosestValidPathFindingHex(orientationTarget.transform.position);

      if (useOrientationTarget) RotateToTarget(lance, orientationTarget);

      if (!useMiniumDistance || IsWithinBoundedDistanceOfTarget(newSpawnPosition, validOrientationTargetPosition, minimumDistance)) {
        if (!AreLanceMemberSpawnsValid(lance, validOrientationTargetPosition)) {
          if (AttemptCount > AttemptCountMax) {  // Attempt to spawn on the selected edge. If it's not possible, select another edge
            edge = RectExtensions.RectEdge.ANY;
            AttemptCount = 0;
            minimumDistance -= 25f;
            if (minimumDistance <= 0) {
              if (vanillaPosition == Vector3.zero) {
                Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] Cannot find valid spawn. Spawning with 'SpawnAnywhere' profile.");
                RunFallbackSpawn(payload, this.lanceKey, this.orientationTargetKey);
              } else {
                lance.transform.position = vanillaPosition;
                Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] Cannot find valid spawn. Spawning at vanilla location for the encounter");
              }
              return;
            }
            Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] Cannot find valid spawn. Selecting a new edge and reducing minimum distance to '{minimumDistance}'");
            Run(payload);
          } else {
            edge = xzEdge.Edge;
            Run(payload);
          }
        } else {
          Main.Logger.Log("[SpawnLanceAtEdgeOfBoundary] Lance spawn complete");
          CorrectLanceMemberSpawns(lance);
        }
      } else {
        Main.Logger.Log("[SpawnLanceAtEdgeOfBoundary] Spawn is too close to the target. Selecting a new spawn.");
        edge = xzEdge.Edge;
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