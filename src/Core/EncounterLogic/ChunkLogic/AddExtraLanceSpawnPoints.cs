using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;
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
      IncreaseLanceSpawnPoints(contractOverride, targetTeamOverride);
    }

    private void IncreaseLanceSpawnPoints(ContractOverride contractOverride, TeamOverride teamOverride) {
      List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;
      int factionLanceSize = Main.Settings.ExtendedLances.GetFactionLanceSize(teamOverride.faction.ToString());

      foreach (LanceOverride lanceOverride in lanceOverrides) {
        int numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;

        if (numberOfUnitsInLance < factionLanceSize) {
          // This is usually from a 'tagged' lance being selected which has less lance members than the faction lance size
          if (Main.Settings.ExtendedLances.Autofill) {
            Main.LogDebug("[AddExtraLanceSpawnPoints] Populated lance has fewer units than the faction requires. Autofilling the missing units");
            AddNewUnits(contractOverride, teamOverride, lanceOverride, numberOfUnitsInLance, factionLanceSize);
          } else {
            Main.LogDebug("[AddExtraLanceSpawnPoints] Populated lance has fewer units than the faction requires. Allowing as a valid setup as 'Autofill' is false");
          }
        }

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

    private void AddNewUnits(ContractOverride contractOverride, TeamOverride teamOverride, LanceOverride lanceOverride, int numberOfUnitsInLance, int factionLanceSize) {
      using (MetadataDatabase metadataDatabase = new MetadataDatabase()) {
        for (int i = numberOfUnitsInLance - 1; i < factionLanceSize; i++) {
          UnitSpawnPointOverride unitSpawnPointOverride = lanceOverride.unitSpawnPointOverrideList[0].Copy();
          unitSpawnPointOverride.GenerateUnit(metadataDatabase, UnityGameInstance.Instance.Game.DataManager, lanceOverride.selectedLanceDifficulty, lanceOverride.name, null, i);
          lanceOverride.unitSpawnPointOverrideList.Add(unitSpawnPointOverride);
        }
      }
    }
  }
}