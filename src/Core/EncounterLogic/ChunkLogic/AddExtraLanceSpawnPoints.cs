using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.EncounterFactories;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class AddExtraLanceSpawnPoints : ChunkLogic {
    private List<LanceSpawnerGameLogic> lanceSpawners;

    public AddExtraLanceSpawnPoints() { }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddExtraLanceSpawnPoints] Adding lance spawn points to match contract override data");
      Contract contract = MissionControl.Instance.CurrentContract;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      ContractOverride contractOverride = contract.Override;

      lanceSpawners = new List<LanceSpawnerGameLogic>(encounterLayerData.gameObject.GetComponentsInChildren<LanceSpawnerGameLogic>());

      TeamOverride targetTeamOverride = contractOverride.targetTeam;
      IncreaseLanceSpawnPoints(targetTeamOverride);
    }

    private void IncreaseLanceSpawnPoints(TeamOverride teamOverride) {
      List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;
      foreach (LanceOverride lanceOverride in lanceOverrides) {
        int numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;
        LanceSpawnerGameLogic lanceSpawner = lanceSpawners.Find(spawner => spawner.GUID == lanceOverride.lanceSpawner.EncounterObjectGuid);
        List<GameObject> unitSpawnPoints = lanceSpawner.gameObject.FindAllContains("UnitSpawnPoint");
        Vector3 lastSpawnPosition = unitSpawnPoints[unitSpawnPoints.Count - 1].transform.localPosition;

        if (lanceSpawner != null) {
          if (numberOfUnitsInLance > unitSpawnPoints.Count) {
            Main.Logger.Log($"[AddExtraLanceSpawnPoints] Detected lance that has more units than vanilla supports. Creating new lance spawns to accommodate.");
            for (int i = 4; i < numberOfUnitsInLance; i++) {
              Vector3 spawnPositon = new Vector3(lastSpawnPosition.x + 24f, lastSpawnPosition.y, lastSpawnPosition.z + 24f);
              LanceSpawnerFactory.CreateUnitSpawnPoint(lanceSpawner.gameObject, $"UnitSpawnPoint{i + 1}", spawnPositon, lanceOverride.unitSpawnPointOverrideList[i].unitSpawnPoint.EncounterObjectGuid);
            }
          }
        } else {
          Main.Logger.LogError($"[AddExtraLanceSpawnPoints] Spawner is null for {lanceOverride.lanceSpawner.EncounterObjectGuid}. This shouldn't be the case.");
        }
      }
    }
  }
}