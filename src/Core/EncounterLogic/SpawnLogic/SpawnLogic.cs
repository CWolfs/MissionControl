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

    public SpawnLogic(EncounterRules encounterRules) : base(encounterRules) { }

    protected bool IsSpawnValid(GameObject spawnPoint, GameObject checkTarget) {
      return IsSpawnValid(spawnPoint, checkTarget.transform.position);
    }

    public Vector3 GetClosestValidPathFindingHex(Vector3 origin) {
      Main.LogDebug($"[GetClosestValidPathFindingHex] About to process with origin '{origin}'");
      Vector3 validOrigin = PathfindFromPointToPlayerSpawn(origin);
      
      if (validOrigin == Vector3.zero) {
        Main.Logger.LogError($"[GetClosestValidPathFindingHex] No valid points found. Reverting to original of '{origin}'");
        return origin;
      }

      Main.LogDebug($"[GetClosestValidPathFindingHex] Returning final point '{validOrigin}'");
      return validOrigin;	
  	}

    private Vector3 PathfindFromPointToPlayerSpawn(Vector3 origin) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Vector3 originOnGrid = origin.GetClosestHexLerpedPointOnGrid();
      Vector3 playerLanceSpawnPosition = EncounterRules.SpawnerPlayerLanceGo.transform.position.GetClosestHexLerpedPointOnGrid();
      Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Starting test on '{originOnGrid}'");

      if (!PathFinderManager.Instance.IsSpawnValid(originOnGrid, playerLanceSpawnPosition, UnitType.Vehicle)) {
        Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Origin of '{originOnGrid}' is not a valid pathfinding location. Finding a point nearby");
        List<Vector3> adjacentPointsOnGrid = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(originOnGrid, 5);
        adjacentPointsOnGrid.Shuffle();
  
        foreach (Vector3 point in adjacentPointsOnGrid) {
          Vector3 validPoint = point.GetClosestHexLerpedPointOnGrid();
          Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Testing point '{validPoint}'");
          if (PathFinderManager.Instance.IsSpawnValid(validPoint, playerLanceSpawnPosition, UnitType.Vehicle)) {
            Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Adjacent spawn has been found valid by IsSpawnValid check for '{validPoint}'");
            return validPoint;
          }
        }

        return Vector3.zero;
      } else {
        Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Spawn has been found valid by pathfinding '{originOnGrid}'");
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