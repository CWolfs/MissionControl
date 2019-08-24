using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;
using BattleTech.Designed;
using BattleTech.Framework;

using HBS.Collections;

using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.EncounterFactories;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class AddExtraPlayerLanceSpawnPoints : ChunkLogic {
    private List<LanceSpawnerGameLogic> lanceSpawners;

    public AddExtraPlayerLanceSpawnPoints() { }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddExtraPlayerLanceSpawnPoints] Adding lance spawn points to match contract override data");
      Contract contract = MissionControl.Instance.CurrentContract;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      ContractOverride contractOverride = contract.Override;

      lanceSpawners = new List<LanceSpawnerGameLogic>(encounterLayerData.gameObject.GetComponentsInChildren<LanceSpawnerGameLogic>());

      TeamOverride playerTeamOverride = contractOverride.player1Team;
      IncreaseLanceSpawnPoints(contract, contractOverride, playerTeamOverride);
    }

    private void IncreaseLanceSpawnPoints(Contract contract, ContractOverride contractOverride, TeamOverride teamOverride) {
      SpawnableUnit[] lanceUnits = contract.Lances.GetLanceUnits(EncounterRules.EMPLOYER_TEAM_ID);

      /*
      foreach (SpawnableUnit lanceUnit in lanceUnits) {
        int numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;
          
        }

        LanceSpawnerGameLogic lanceSpawner = lanceSpawners.Find(spawner => spawner.GUID == lanceOverride.lanceSpawner.EncounterObjectGuid);
        if (lanceSpawner != null) {
          List<GameObject> unitSpawnPoints = lanceSpawner.gameObject.FindAllContains("UnitSpawnPoint");
          numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;

          if (numberOfUnitsInLance > unitSpawnPoints.Count) {
            Main.Logger.Log($"[AddExtraPlayerLanceSpawnPoints] Detected lance that has more units than vanilla supports. Creating new lance spawns to accommodate.");
            for (int i = 4; i < numberOfUnitsInLance; i++) {
              Vector3 randomLanceSpawn = unitSpawnPoints.GetRandom().transform.localPosition;
              Vector3 spawnPositon = new Vector3(randomLanceSpawn.x + 24f, randomLanceSpawn.y, randomLanceSpawn.z + 24f);
              LanceSpawnerFactory.CreateUnitSpawnPoint(lanceSpawner.gameObject, $"UnitSpawnPoint{i + 1}", spawnPositon, lanceOverride.unitSpawnPointOverrideList[i].unitSpawnPoint.EncounterObjectGuid);
            }
          }
        } else {
          Main.Logger.LogWarning($"[AddExtraPlayerLanceSpawnPoints] Spawner is null for {lanceOverride.lanceSpawner.EncounterObjectGuid}. This is probably data from a restarted contract that hasn't been cleared up. It can be safely ignored.");
        }
      }
      */
    }
  }
}