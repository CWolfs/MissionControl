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

    protected bool IsSpawnValid(GameObject spawnPoint, GameObject checkTarget) {
      return IsSpawnValid(spawnPoint, checkTarget.transform.position);
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