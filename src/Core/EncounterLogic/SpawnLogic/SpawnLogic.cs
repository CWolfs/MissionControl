using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public abstract class SpawnLogic : SceneManipulationLogic {
    public SpawnLogic(EncounterRules encounterRules) : base(encounterRules) { }

    protected bool IsSpawnValid(GameObject spawnPoint, GameObject orientationTarget) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      Vector3 spawnPointPosition = combatState.HexGrid.GetClosestPointOnGrid(spawnPoint.transform.position);
      spawnPointPosition.y = combatState.MapMetaData.GetLerpedHeightAt(spawnPointPosition);

      Vector3 checkTarget = combatState.HexGrid.GetClosestPointOnGrid(orientationTarget.transform.position);
      checkTarget.y = combatState.MapMetaData.GetLerpedHeightAt(checkTarget);
      
      if (!PathFinderManager.Instance.IsSpawnValid(spawnPointPosition, checkTarget, UnitType.Vehicle)) {
        Main.Logger.LogWarning("[IsSpawnValid] Spawn path to first objective is blocked. Select a new spawn point");
        return false;
      }
      
      PathFinderManager.Instance.Reset();
      return true;
    }
  }
}