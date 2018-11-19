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
      return IsSpawnValid(spawnPoint, checkTarget.transform.position.GetClosestHexLerpedPointOnGrid());
    }

    public Vector3 GetClosestValidPathFindingHex(Vector3 origin, int radius = 3) {
      Main.LogDebug($"[GetClosestValidPathFindingHex] About to process with origin '{origin}'");
      Vector3 validOrigin = PathfindFromPointToPlayerSpawn(origin, radius);
      
      if (validOrigin == Vector3.zero) {
        origin = origin.GetClosestHexLerpedPointOnGrid();
        Main.LogDebugWarning($"[GetClosestValidPathFindingHex] No valid points found. Reverting to original with fixed height of '{origin}'");
        return origin;
      }

      Main.LogDebug($"[GetClosestValidPathFindingHex] Returning final point '{validOrigin}'");
      return validOrigin;	
  	}

    private Vector3 PathfindFromPointToPlayerSpawn(Vector3 origin, int radius) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Vector3 originOnGrid = origin.GetClosestHexLerpedPointOnGrid();
      Vector3 playerLanceSpawnPosition = EncounterRules.SpawnerPlayerLanceGo.transform.position.GetClosestHexLerpedPointOnGrid();

      if (!PathFinderManager.Instance.IsSpawnValid(originOnGrid, playerLanceSpawnPosition, UnitType.Vehicle)) {
        List<Vector3> adjacentPointsOnGrid = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(originOnGrid, radius);
        Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Adjacent point count is '{adjacentPointsOnGrid.Count}'");
        adjacentPointsOnGrid.Shuffle();
  
        foreach (Vector3 point in adjacentPointsOnGrid) {
          Vector3 validPoint = point.GetClosestHexLerpedPointOnGrid();
          if (PathFinderManager.Instance.IsSpawnValid(validPoint, playerLanceSpawnPosition, UnitType.Vehicle)) {
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
        Main.LogDebug($"[IsSpawnValid] [Spawn point {spawnPoint.name}] Spawn path at '{spawnPointPosition}' to check target ({checkTarget}) is blocked. Select a new spawn point");
        return false;
      }
      
      PathFinderManager.Instance.Reset();
      return true;
    }
  }
}