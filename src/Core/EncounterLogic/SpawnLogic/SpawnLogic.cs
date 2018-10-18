using UnityEngine;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public abstract class SpawnLogic : SceneManipulationLogic {
    private List<Vector3> checkedPoints = null;

    public SpawnLogic(EncounterRules encounterRules) : base(encounterRules) { }

    protected bool IsSpawnValid(GameObject spawnPoint, GameObject checkTarget) {
      return IsSpawnValid(spawnPoint, checkTarget.transform.position);
    }

    public Vector3 GetClosestValidPathFindingHex(Vector3 origin) {
      Main.LogDebug($"[GetClosestValidPathFindingHex] About to process with origin '{origin}'");
      checkedPoints = new List<Vector3>();
      Vector3 validOrigin = PathfindFromPointToPlayerSpawn(origin, 0, 5);
      
      if (validOrigin == Vector3.zero) {
        Main.Logger.LogError($"[GetClosestValidPathFindingHex] No valid points found. Reverting to original of '{origin}'");
        return origin;
      }

      Main.LogDebug($"[GetClosestValidPathFindingHex] Returning final point '{validOrigin}'");
      return validOrigin;	
  	}

    private Vector3 PathfindFromPointToPlayerSpawn(Vector3 origin, int depth = 0, int maxDepth = 0) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Vector3 originOnGrid = origin.GetClosestHexLerpedPointOnGrid();
      Vector3 playerLanceSpawnPosition = EncounterRules.SpawnerPlayerLanceGo.transform.position.GetClosestHexLerpedPointOnGrid();
      checkedPoints.Add(originOnGrid);
      // Main.LogDebug($"[PathfindFromPointToPlayerSpawn - depth '{depth}'] Starting test on '{originOnGrid}'");

      if (!PathFinderManager.Instance.IsSpawnValid(originOnGrid, playerLanceSpawnPosition, UnitType.Vehicle)) {
        // Main.LogDebug($"[PathfindFromPointToPlayerSpawn - depth '{depth}'] Origin of '{originOnGrid}' is not a valid pathfinding location. Finding a point nearby");
        List<Vector3> adjacentPointsOnGrid = combatState.HexGrid.GetAdjacentPointsOnGrid(originOnGrid);
        adjacentPointsOnGrid = adjacentPointsOnGrid.Except(checkedPoints).ToList();
        adjacentPointsOnGrid.Shuffle();
  
        foreach (Vector3 point in adjacentPointsOnGrid) {
          Vector3 validPoint = point.GetClosestHexLerpedPointOnGrid();
          // Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Testing point '{validPoint}'");
          if (PathFinderManager.Instance.IsSpawnValid(validPoint, playerLanceSpawnPosition, UnitType.Vehicle)) {
            return validPoint;
          }
        }

        // If all points were not valid, go through and check their adajcent ones
        if (depth < maxDepth) {
          // Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Heading a depth lower from {depth} to {depth+1}");
          foreach (Vector3 point in adjacentPointsOnGrid) {
            originOnGrid = PathfindFromPointToPlayerSpawn(point, depth + 1, maxDepth);
            if (originOnGrid != Vector3.zero) return originOnGrid;
          }
          return Vector3.zero;
        } else {
          return Vector3.zero;
        }
      }

      return originOnGrid;
    }

    protected bool IsSpawnValid(GameObject spawnPoint, Vector3 checkTargetPosition) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Vector3 spawnPointPosition = spawnPoint.transform.position.GetClosestHexLerpedPointOnGrid();
      Vector3 checkTarget = checkTargetPosition.GetClosestHexLerpedPointOnGrid();
      
      if (!PathFinderManager.Instance.IsSpawnValid(spawnPointPosition, checkTarget, UnitType.Vehicle)) {
        Main.Logger.LogWarning($"[IsSpawnValid] [Spawn point {spawnPoint.name}] Spawn path at '{spawnPointPosition}' to check target ({checkTarget}) is blocked. Select a new spawn point");
        return false;
      }
      
      PathFinderManager.Instance.Reset();
      return true;
    }
  }
}