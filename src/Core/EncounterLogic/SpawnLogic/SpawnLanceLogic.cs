using UnityEngine;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public abstract class SpawnLanceLogic : SpawnLogic {
    protected float minDistanceToSpawnFromInvalidSpawn = 10f;


    public SpawnLanceLogic(EncounterRules encounterRules) : base(encounterRules) { }

    protected void CorrectLanceMemberSpawns(GameObject lance) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");

      foreach (GameObject spawnPoint in spawnPoints) {
        Vector3 spawnPointPosition = spawnPoint.transform.position.GetClosestHexLerpedPointOnGrid();
        spawnPoint.transform.position = spawnPointPosition;
      }
    }

    protected void RunFallbackSpawn(RunPayload payload, string lanceKey, string orientationTargetKey) {
      SpawnLanceAnywhere spawnAnywhere = new SpawnLanceAnywhere(EncounterRules, lanceKey, orientationTargetKey);
      spawnAnywhere.Run(payload);
    }

    protected bool AreLanceMemberSpawnsValid(GameObject lance, Vector3 checkTarget) {
      List<GameObject> invalidLanceSpawns = GetInvalidLanceMemberSpawns(lance, checkTarget);
      if (invalidLanceSpawns.Count <= 0) return true;
      return false;
    }

    protected List<GameObject> GetInvalidLanceMemberSpawns(GameObject lance, Vector3 checkTarget) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;

      List<GameObject> invalidLanceSpawns = new List<GameObject>();
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");
      Vector3 checkTargetPosition = checkTarget.GetClosestHexLerpedPointOnGrid();

      foreach (GameObject spawnPoint in spawnPoints) {
        Vector3 spawnPointPosition = spawnPoint.transform.position.GetClosestHexLerpedPointOnGrid();
        Main.LogDebug($"[GetInvalidLanceMemberSpawns] Spawn point's closest hex lerped point on grid for '{spawnPoint.name}' is '{spawnPointPosition}'");
        
        if (!encounterLayerData.IsInEncounterBounds(spawnPointPosition)) {
          Main.LogDebugWarning("[GetInvalidLanceMemberSpawns] Lance member spawn is outside of the boundary. Select a new lance spawn point.");
          invalidLanceSpawns.Add(spawnPoint);
          continue; 
        }

        // Ensure the lance member's spawn's closest valid point isn't on another spawn point's closest valid point
        if (IsPointTooCloseToOtherPointsClosestPointOnGrid(spawnPointPosition, spawnPoints.Where(sp => spawnPoint.name != sp.name).ToList())) {
          Main.LogDebugWarning("[GetInvalidLanceMemberSpawns] Lance member spawn is too close to the other spawns when snapped to the grid");
          invalidLanceSpawns.Add(spawnPoint);
          continue;
        }

        if (!PathFinderManager.Instance.IsSpawnValid(spawnPointPosition, checkTargetPosition, UnitType.Vehicle)) {
          Main.LogDebugWarning($"[GetInvalidLanceMemberSpawns] Lance member spawn '{spawnPoint.name}' path to check target '{checkTarget}' is blocked. Select a new lance spawn point");
          invalidLanceSpawns.Add(spawnPoint);
          continue;
        }

        spawnPoint.transform.position = spawnPointPosition;
      }

      PathFinderManager.Instance.Reset();
      return invalidLanceSpawns;
    }

    protected bool IsPointTooCloseToOtherPointsClosestPointOnGrid(Vector3 point, List<GameObject> points) {
      List<Vector3> vectorPoints = new List<Vector3>();
      for (int i = 0; i < points.Count; i++) {
        vectorPoints.Add(points[i].transform.position);
      }
      return IsPointTooCloseToOtherPointsClosestPointOnGrid(point, vectorPoints);
    }

    protected bool IsPointTooCloseToOtherPointsClosestPointOnGrid(Vector3 point, List<Vector3> points) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      foreach (Vector3 checkPoint in points) {
        Vector3 validCheckPoint = combatState.HexGrid.GetClosestPointOnGrid(checkPoint);
        Vector3 vectorToCheckPoint = point - validCheckPoint;
        vectorToCheckPoint.y = 0;
        float distanceBetweenPoints = vectorToCheckPoint.magnitude;
        if (distanceBetweenPoints < minDistanceToSpawnFromInvalidSpawn) return true;
      }
      return false;
    }    
  }
}