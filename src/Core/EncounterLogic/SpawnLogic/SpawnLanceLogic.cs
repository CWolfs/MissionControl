using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using ContractCommand.Rules;

namespace ContractCommand.Logic {
  public abstract class SpawnLanceLogic : SpawnLogic {
    protected EncounterRule EncounterRule { get; set; }

    public SpawnLanceLogic(EncounterRule encounterRule) : base() {
      EncounterRule = encounterRule;
    }

    protected bool AreLanceMemberSpawnsValid(GameObject lance, GameObject orientationTarget) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");

      foreach (GameObject spawnPoint in spawnPoints) {
        Vector3 spawnPointPosition = combatState.HexGrid.GetClosestPointOnGrid(spawnPoint.transform.position);
        spawnPointPosition.y = combatState.MapMetaData.GetLerpedHeightAt(spawnPointPosition);

        Vector3 checkTarget = combatState.HexGrid.GetClosestPointOnGrid(orientationTarget.transform.position);
        checkTarget.y = combatState.MapMetaData.GetLerpedHeightAt(checkTarget);
        
        if (!PathFinderManager.GetInstance().IsSpawnValid(spawnPointPosition, checkTarget)) {
          Main.Logger.LogWarning("[AreLanceMemberSpawnsValid] Lance member spawn path to first objective is blocked. Select a new lance spawn point");
          return false;
        }
      }

      PathFinderManager.GetInstance().Reset();
      return true;
    }
  }
}