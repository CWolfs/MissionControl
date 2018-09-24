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
    private string lanceKey;
    private string orientationTargetKey;

    private GameObject lance;
    private GameObject orientationTarget;
    private RectExtensions.RectEdge edge = RectExtensions.RectEdge.ANY;

    private int AttemptCountMax { get; set; } = 10;
    private int AttemptCount { get; set; } = 0;

    public SpawnLanceAtEdgeOfBoundary(EncounterRule encounterRule, string lanceKey, string orientationTargetKey) : base(encounterRule) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] For {lance.name}");

      AttemptCount++;
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      EncounterManager EncounterManager = EncounterManager.GetInstance();
      GameObject chunkBoundaryRect = EncounterManager.EncounterLayerGameObject.transform.Find("Chunk_EncounterBoundary").gameObject;
      GameObject boundary = chunkBoundaryRect.transform.Find("EncounterBoundaryRect").gameObject;
      EncounterBoundaryChunkGameLogic chunkBoundary = chunkBoundaryRect.GetComponent<EncounterBoundaryChunkGameLogic>();
      EncounterBoundaryRectGameLogic boundaryLogic = boundary.GetComponent<EncounterBoundaryRectGameLogic>();
      Rect boundaryRec = chunkBoundary.GetEncounterBoundaryRectBounds();
      RectEdgePosition xzEdge = boundaryRec.CalculateRandomXZEdge(boundary.transform.position, edge);

      Vector3 lancePosition = lance.transform.position;
      Vector3 newSpawnPosition = new Vector3(xzEdge.Position.x, lancePosition.y, xzEdge.Position.z);
      newSpawnPosition.y = combatState.MapMetaData.GetLerpedHeightAt(newSpawnPosition);

      lance.transform.position = newSpawnPosition;
      RotateToTarget(lance, orientationTarget);

      if (!AreLanceMemberSpawnsValid(lance, orientationTarget)) {
        if (AttemptCount > AttemptCountMax) {  // Attempt to spawn on the selected edge. If it's not possible, select another edge
          edge = RectExtensions.RectEdge.ANY;
          AttemptCount = 0;
          Run(payload);
        } else {
          edge = xzEdge.Edge;
          Run(payload);
        }
      } else {
        Main.Logger.Log("[SpawnLanceAtEdgeOfBoundary] Lance spawn complete");
      }
    }

    protected override void GetObjectReferences() {
      this.EncounterRule.ObjectLookup.TryGetValue(lanceKey, out lance);
      this.EncounterRule.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (lance == null || orientationTarget == null) {
        Main.Logger.LogError("[SpawnLanceAroundTarget] Object referneces are null");
      }
    }
  }
}