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
    
    private int ADJACENT_NODE_LIMITED = 30;

    public SpawnLogic(EncounterRules encounterRules) : base(encounterRules) { }

    protected bool IsSpawnValid(GameObject spawnPoint, GameObject checkTarget) {
      return IsSpawnValid(spawnPoint, checkTarget.transform.position.GetClosestHexLerpedPointOnGrid());
    }

    public Vector3 GetClosestValidPathFindingHex(Vector3 origin, string identifier, int radius = 3) {
      return GetClosestValidPathFindingHex(origin, identifier, Vector3.zero, radius);
    }

    public Vector3 GetClosestValidPathFindingHex(Vector3 origin, string identifier, Vector3 pathfindingTarget, int radius = 3) {
      Main.LogDebug($"[GetClosestValidPathFindingHex] About to process with origin '{origin}'");
      Vector3 validOrigin = PathfindFromPointToSpawn(origin, radius, identifier, pathfindingTarget);

      // Fallback to original position if a search of 8 nodes radius turns up no valid path
      if (radius > 8) {
        origin = origin.GetClosestHexLerpedPointOnGrid();
        Main.LogDebugWarning($"[GetClosestValidPathFindingHex] No valid points found. Reverting to original with fixed height of '{origin}'");
        return origin;
      }
      
      if (validOrigin == Vector3.zero) {
        Main.LogDebugWarning($"[GetClosestValidPathFindingHex] No valid points found. Expanding search radius from radius '{radius}' to '{radius * 2}'");
        origin = GetClosestValidPathFindingHex(origin, identifier, pathfindingTarget, radius * 2);
        return origin;
      }

      Main.LogDebug($"[GetClosestValidPathFindingHex] Returning final point '{validOrigin}'");
      return validOrigin;	
  	}

    private Vector3 PathfindFromPointToSpawn(Vector3 origin, int radius, string identifier, Vector3 pathfindingTarget) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Vector3 originOnGrid = origin.GetClosestHexLerpedPointOnGrid();
      // TODO: If the SpawnerPlayerLanceGo's closest hex point is in an inaccessible location - this will cause infinite loading issues
      // TODO: Need to find a reliably accessible location (hard to do in a proc-genned setup)
      Vector3 pathfindingPoint = (pathfindingTarget == Vector3.zero) ? 
        EncounterRules.SpawnerPlayerLanceGo.transform.position.GetClosestHexLerpedPointOnGrid()
      : pathfindingTarget.GetClosestHexLerpedPointOnGrid();


      if (!PathFinderManager.Instance.IsSpawnValid(originOnGrid, pathfindingPoint, UnitType.Mech, identifier)) {
        List<Vector3> adjacentPointsOnGrid = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(originOnGrid, radius);
        Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Adjacent point count is '{adjacentPointsOnGrid.Count}'");
        adjacentPointsOnGrid.Shuffle();
  
        int count = 0;
        foreach (Vector3 point in adjacentPointsOnGrid) {
          if (count > ADJACENT_NODE_LIMITED) {
            Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Adjacent point count limited exceeded ({adjacentPointsOnGrid.Count} / {ADJACENT_NODE_LIMITED}). Bailing.");  
            break;
          }

          Vector3 validPoint = point.GetClosestHexLerpedPointOnGrid();
          if (PathFinderManager.Instance.IsSpawnValid(validPoint, pathfindingPoint, UnitType.Mech, identifier)) {
            return validPoint;
          }
          count++;
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
      
      if (!PathFinderManager.Instance.IsSpawnValid(spawnPointPosition, checkTarget, UnitType.Mech, spawnPoint.name)) {
        Main.LogDebug($"[IsSpawnValid] [Spawn point {spawnPoint.name}] Spawn path at '{spawnPointPosition}' to check target ({checkTarget}) is blocked. Select a new spawn point");
        return false;
      }
      
      return true;
    }
  }
}