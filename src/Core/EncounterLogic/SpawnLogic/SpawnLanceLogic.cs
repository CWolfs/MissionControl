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
        Vector3 spawnPointPosition = combatState.HexGrid.GetClosestPointOnGrid(spawnPoint.transform.position);
        spawnPointPosition.y = combatState.MapMetaData.GetLerpedHeightAt(spawnPointPosition);
        spawnPoint.transform.position = spawnPointPosition;
      }
    }

    protected bool AreLanceMemberSpawnsValid(GameObject lance, GameObject checkTarget) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");

      foreach (GameObject spawnPoint in spawnPoints) {
        Vector3 spawnPointPosition = spawnPoint.transform.position.GetClosestHexLerpedPointOnGrid();

        // Ensure the lance member's spawn's closest valid point isn't on another spawn point's closest valid point
        if (IsPointTooCloseToOtherPointsClosestPointOnGrid(spawnPointPosition, spawnPoints.Where(sp => spawnPoint.name != sp.name).ToList())) {
          Main.Logger.LogWarning("[AreLanceMemberSpawnsValid] Lance member spawn is too close to the other spawns when snapped to the grid");
          return false;
        }

        Vector3 checkTargetPosition = checkTarget.transform.position.GetClosestHexLerpedPointOnGrid();
        if (!PathFinderManager.Instance.IsSpawnValid(spawnPointPosition, checkTargetPosition, UnitType.Vehicle)) {
          Main.Logger.LogWarning($"[AreLanceMemberSpawnsValid] Lance member spawn '{spawnPoint.name}' path to check target '{checkTarget.name}' is blocked. Select a new lance spawn point");
          return false;
        }

        EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
        if (!encounterLayerData.IsInEncounterBounds(spawnPointPosition)) {
          Main.Logger.LogWarning("[AreLanceMemberSpawnsValid] Lance member spawn is outside of the boundary. Select a new lance spawn point.");
          return false;  
        }
      }

      PathFinderManager.Instance.Reset();
      return true;
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