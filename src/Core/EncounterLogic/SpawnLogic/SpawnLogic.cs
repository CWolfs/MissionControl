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
    public float START_TIME_FOR_SPAWN_LOGIC = 0;
    protected bool shouldGracefullyStopSpawnLogic = false;
    public static Vector3 TimedOutVector = new Vector3(99999, 99999, 99999);

    private int ADJACENT_NODE_LIMITED = 20;
    private List<Vector3> checkedAdjacentPoints = new List<Vector3>();
    private int BAILOUT_MAX = 2;
    private int bailoutCount = 0;
    private Vector3 originalOrigin = Vector3.zero;

    public SpawnLogic(EncounterRules encounterRules) : base(encounterRules) {
      START_TIME_FOR_SPAWN_LOGIC = Time.realtimeSinceStartup;
    }

    private bool HasSpawnLogicExceededTimeBudget() {
      int maxSecondsUntilTimeout = Main.Settings.RandomSpawns.TimeoutIn;
      Main.LogDebug($"[CheckSpawnLogicTimeBudget] Checking spawn logic time budget");

      float timeUsed = Time.realtimeSinceStartup - START_TIME_FOR_SPAWN_LOGIC;
      if (maxSecondsUntilTimeout < timeUsed) {
        return true;
      }

      return false;
    }

    public bool IsTimedOut(Vector3 vector) {
      if (vector == TimedOutVector) return true;
      return false;
    }

    protected bool HasSpawnerTimedOut() {
      if (shouldGracefullyStopSpawnLogic) {
        Main.Logger.LogDebug($"[{this.GetType().Name}] Has timed out. Gracefully stopping spawn.");
        return shouldGracefullyStopSpawnLogic;
      }

      return false;
    }

    private void StopSpawnLogic() {
      Main.LogDebug($"[CheckSpawnLogicTimeBudget] Spawn budget of '{Main.Settings.RandomSpawns.TimeoutIn}' seconds exceeded. Gracefully attempting to stop spawn logic and using a fallback.");
      shouldGracefullyStopSpawnLogic = true;

      // Restore original spawn points
    }

    protected bool IsSpawnValid(GameObject spawnPoint, GameObject checkTarget) {
      return IsSpawnValid(spawnPoint, checkTarget.transform.position.GetClosestHexLerpedPointOnGrid());
    }

    public Vector3 GetClosestValidPathFindingHex(GameObject originGo, Vector3 origin, string identifier, int radius = 3) {
      return GetClosestValidPathFindingHex(originGo, origin, identifier, Vector3.zero, radius);
    }

    public Vector3 GetClosestValidPathFindingHex(GameObject originGo, Vector3 origin, string identifier, Vector3 pathfindingTarget, int radius = 3) {
      if (HasSpawnLogicExceededTimeBudget()) {
        StopSpawnLogic();
        return TimedOutVector;
      }

      Main.LogDebug($"[GetClosestValidPathFindingHex] About to process with origin '{origin}'");
      if (originalOrigin == Vector3.zero) originalOrigin = origin;

      // If no valid pathfinds can be made then the pathfind target or origin might be bad
      // Get a new point for both relatively close to the original points to test against and try again.
      if (radius > 12) {
        if (bailoutCount >= BAILOUT_MAX) {
          origin = originalOrigin.GetClosestHexLerpedPointOnGrid();
          Main.LogDebugWarning($"[GetClosestValidPathFindingHex] No valid points found. Returning original origin with fixed height of '{origin}'");
          checkedAdjacentPoints.Clear();
          bailoutCount = 0;
          originalOrigin = Vector3.zero;
          return origin;
        } else {
          bailoutCount++;
          Main.LogDebugWarning($"[GetClosestValidPathFindingHex] No valid points found. Casting net out to another location'");
          radius = 3;
          // Vector3 randomPositionFromBadSpawn = SceneUtils.GetRandomPositionWithinBounds(origin, 96);
          // Main.LogDebugWarning($"[GetClosestValidPathFindingHex] New location to test for '{originGo.name}' is '{randomPositionFromBadSpawn}'");

          Vector3 randomPositionFromBadPathfindTarget = SceneUtils.GetRandomPositionWithinBounds(pathfindingTarget, 96);
          Main.LogDebugWarning($"[GetClosestValidPathFindingHex] New location to test for pathfind target is '{randomPositionFromBadPathfindTarget}'");

          // origin = randomPositionFromBadSpawn;
          pathfindingTarget = randomPositionFromBadPathfindTarget;
        }
      }

      Vector3 validOrigin = PathfindFromPointToSpawn(originGo, origin, radius, identifier, pathfindingTarget);

      if (validOrigin == Vector3.zero) {
        Main.LogDebugWarning($"[GetClosestValidPathFindingHex] No valid points found. Expanding search radius from radius '{radius}' to '{radius * 2}'");
        origin = GetClosestValidPathFindingHex(originGo, origin, identifier, pathfindingTarget, radius * 2);
        checkedAdjacentPoints.Clear();
        return origin;
      }

      Main.LogDebug($"[GetClosestValidPathFindingHex] Returning final point '{validOrigin}'");
      checkedAdjacentPoints.Clear();
      return validOrigin;
    }

    private Vector3 PathfindFromPointToSpawn(GameObject originGo, Vector3 origin, int radius, string identifier, Vector3 pathfindingTarget) {
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Vector3 originOnGrid = origin.GetClosestHexLerpedPointOnGrid();

      // TODO: If the SpawnerPlayerLanceGo's closest hex point is in an inaccessible location - this will cause infinite loading issues
      // TODO: Need to find a reliably accessible location (hard to do in a proc-genned setup)
      Vector3 pathfindingPoint = (pathfindingTarget == Vector3.zero) ?
        EncounterRules.SpawnerPlayerLanceGo.transform.position.GetClosestHexLerpedPointOnGrid()
      : pathfindingTarget.GetClosestHexLerpedPointOnGrid();

      Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Using pathfinding point '{pathfindingPoint}'");

      if (!PathFinderManager.Instance.IsSpawnValid(originGo, originOnGrid, pathfindingPoint, UnitType.Mech, identifier)) {
        List<Vector3> adjacentPointsOnGrid = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(originOnGrid, radius);
        Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Adjacent point count is '{adjacentPointsOnGrid.Count}'");

        adjacentPointsOnGrid = adjacentPointsOnGrid.Where(point => {
          return !checkedAdjacentPoints.Contains(point) && encounterLayerData.IsInEncounterBounds(point);
        }).ToList();

        Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Removed already checked points & out of bounds points. Adjacent point count is now '{adjacentPointsOnGrid.Count}'");

        adjacentPointsOnGrid.Shuffle();

        int count = 0;
        foreach (Vector3 point in adjacentPointsOnGrid) {
          if (count > ADJACENT_NODE_LIMITED) {
            Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Adjacent point count limited exceeded (random selection of {ADJACENT_NODE_LIMITED} / {adjacentPointsOnGrid.Count}). Bailing.");
            break;
          }

          Vector3 validPoint = point.GetClosestHexLerpedPointOnGrid();

          Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Testing an adjacent point of '{validPoint}'");
          if (PathFinderManager.Instance.IsSpawnValid(originGo, validPoint, pathfindingPoint, UnitType.Mech, identifier)) {
            return validPoint;
          }

          bool isBadPathfindTest = PathFinderManager.Instance.IsProbablyABadPathfindTest(pathfindingPoint);
          if (isBadPathfindTest) {
            Main.LogDebug($"[PathfindFromPointToPlayerSpawn] Estimated this is a bad pathfind setup so trying something new.");
            radius = 100;
            count = ADJACENT_NODE_LIMITED;
          }

          checkedAdjacentPoints.Add(point);
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

      if (!PathFinderManager.Instance.IsSpawnValid(spawnPoint, spawnPointPosition, checkTarget, UnitType.Mech, spawnPoint.name)) {
        Main.LogDebug($"[IsSpawnValid] [Spawn point {spawnPoint.name}] Spawn path at '{spawnPointPosition}' to check target ({checkTarget}) is blocked. Select a new spawn point");
        return false;
      }

      return true;
    }
  }
}