using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public abstract class SpawnLogic : SceneManipulationLogic {
    public SpawnLogic(EncounterRules encounterRules) : base(encounterRules) { }

    protected Vector3 GetValidLocation(Vector3 origin, Vector3 knownValidLocation, bool recursive = true) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Vector3 originOnGrid = combatState.HexGrid.GetClosestPointOnGrid(origin);
      originOnGrid.y = combatState.MapMetaData.GetLerpedHeightAt(originOnGrid);

      Vector3 rayCastOrigin = new Vector3(originOnGrid.x, originOnGrid.y + 300f, originOnGrid.z);

      RaycastHit hit;
      if (Physics.Raycast(rayCastOrigin, Vector3.down, out hit, 300f, LayerTools.EverythingBut())) {
        // Invalid location, find a valid location
        Main.LogDebug($"[GetValidLocation] The location '{origin}' is not valid after a raycast. The ray collided at '{hit.point}' instead. Finding a valid location.");
        originOnGrid = combatState.HexGrid.GetClosestPointOnGrid(hit.point);
        originOnGrid.y = hit.point.y;
        Main.LogDebug($"[GetValidLocation] Raycast location gives '{originOnGrid}'");
      }

      if (!PathFinderManager.Instance.IsSpawnValid(originOnGrid, originOnGrid, UnitType.Vehicle)) {
        Main.Logger.LogDebug($"[GetValidLocation] Spawn path at '{originOnGrid}' to known valid location '{knownValidLocation}' is blocked. Select a new spawn point");
        
        //List<Vector3> possibleSpawnPoints = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(originOnGrid, 25);
        if (recursive) {
          List<Vector3> possibleSpawnPoints = combatState.HexGrid.GetAdjacentPointsOnGrid(originOnGrid);
          foreach (Vector3 possibleSpawnPoint in possibleSpawnPoints) {
            Main.LogDebug($"[GetValidLocation] Using adjacent spawn point '{possibleSpawnPoint}'");
            Vector3 validOriginOnGrid = GetValidLocation(possibleSpawnPoint, knownValidLocation, false);
            if (validOriginOnGrid != Vector3.zero) return validOriginOnGrid;
          }
        }

        return Vector3.zero; 
      }

      return originOnGrid;
    }

    protected bool IsSpawnValid(GameObject spawnPoint, GameObject orientationTarget) {
      return IsSpawnValid(spawnPoint, orientationTarget.transform.position);
    }

    protected bool IsSpawnValid(GameObject spawnPoint, Vector3 orientationTargetPosition) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Vector3 spawnPointPosition = combatState.HexGrid.GetClosestPointOnGrid(spawnPoint.transform.position);
      spawnPointPosition.y = combatState.MapMetaData.GetLerpedHeightAt(spawnPointPosition);

      Vector3 checkTarget = combatState.HexGrid.GetClosestPointOnGrid(orientationTargetPosition);
      checkTarget.y = combatState.MapMetaData.GetLerpedHeightAt(checkTarget);
      
      if (!PathFinderManager.Instance.IsSpawnValid(spawnPointPosition, checkTarget, UnitType.Vehicle)) {
        Main.Logger.LogWarning($"[IsSpawnValid] [Spawn point {spawnPoint.name}] Spawn path at '{spawnPointPosition.ToString()}' to first objective is blocked. Select a new spawn point");
        return false;
      }
      
      PathFinderManager.Instance.Reset();
      return true;
    }
  }
}