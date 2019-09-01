using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

using HBS.Collections;

using MissionControl.EncounterFactories;

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
      TeamOverride employerTeamOverride = contractOverride.employerTeam;

      IncreaseLanceSpawnPoints(contractOverride, targetTeamOverride);
      IncreaseLanceSpawnPoints(contractOverride, employerTeamOverride);
    }

    private void IncreaseLanceSpawnPoints(ContractOverride contractOverride, TeamOverride teamOverride) {
      List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;
      int factionLanceSize = Main.Settings.ExtendedLances.GetFactionLanceSize(teamOverride.faction.ToString());

      foreach (LanceOverride lanceOverride in lanceOverrides) {
        bool isManualLance = lanceOverride.lanceDefId == "Manual";
        int numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;

        if (lanceOverride.IsATurretLance()) {
          Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected a turret lance Ignoring for Extended Lances.");
          continue;  
        }

        if (isManualLance && numberOfUnitsInLance <= 0) {
          Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected a lance that is set to manual but no units were manually specified. This is a bad contract json setup. Fix it! Ignoring for Extended Lances");
          continue;
        }

        ApplyDifficultyMod(teamOverride, lanceOverride);

        if ((numberOfUnitsInLance < factionLanceSize) && numberOfUnitsInLance > 0) {
          // This is usually from a 'tagged' lance being selected which has less lance members than the faction lance size
          if (Main.Settings.ExtendedLances.Autofill ) {
            Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Populated lance '{lanceOverride.name}' has fewer units than the faction requires. Autofilling the missing units");
            AddNewLanceMembers(contractOverride, teamOverride, lanceOverride, numberOfUnitsInLance, factionLanceSize);
          } else {
            Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Populated lance '{lanceOverride.name}' has fewer units than the faction requires. Allowing as a valid setup as 'Autofill' is false");
          }
        }

        LanceSpawnerGameLogic lanceSpawner = lanceSpawners.Find(spawner => spawner.GUID == lanceOverride.lanceSpawner.EncounterObjectGuid);
        if (lanceSpawner != null) {
          List<GameObject> unitSpawnPoints = lanceSpawner.gameObject.FindAllContains("UnitSpawnPoint");
          numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;

          if (numberOfUnitsInLance > unitSpawnPoints.Count) {
            Main.Logger.Log($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected lance '{lanceOverride.name}' has more units than lance spawn points. Creating new lance spawns to accommodate.");
            for (int i = unitSpawnPoints.Count; i < numberOfUnitsInLance; i++) {
              Vector3 randomLanceSpawn = unitSpawnPoints.GetRandom().transform.localPosition;
              Vector3 spawnPositon = SceneUtils.GetRandomPositionFromTarget(randomLanceSpawn, 24, 100);
              spawnPositon = spawnPositon.GetClosestHexLerpedPointOnGrid();
                            
              Main.Logger.Log($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Creating lance '{lanceOverride.name}' spawn point 'UnitSpawnPoint{i + 1}'");
              LanceSpawnerFactory.CreateUnitSpawnPoint(lanceSpawner.gameObject, $"UnitSpawnPoint{i + 1}", spawnPositon, lanceOverride.unitSpawnPointOverrideList[i].unitSpawnPoint.EncounterObjectGuid);
            }
          }
        } else {
          Main.Logger.LogWarning($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Spawner is null for {lanceOverride.lanceSpawner.EncounterObjectGuid}. This is probably data from a restarted contract that hasn't been cleared up. It can be safely ignored.");
        }
      }
    }

    private void AddNewLanceMembers(ContractOverride contractOverride, TeamOverride teamOverride, LanceOverride lanceOverride, int numberOfUnitsInLance, int factionLanceSize) {
      for (int i = numberOfUnitsInLance; i < factionLanceSize; i++) {
        UnitSpawnPointOverride originalUnitSpawnPointOverride = lanceOverride.GetAnyTaggedLanceMember();
        if (originalUnitSpawnPointOverride == null) originalUnitSpawnPointOverride = lanceOverride.unitSpawnPointOverrideList[0];
        UnitSpawnPointOverride unitSpawnPointOverride = originalUnitSpawnPointOverride.DeepCopy();
        TagSet companyTags = new TagSet(UnityGameInstance.BattleTechGame.Simulation.CompanyTags);

        unitSpawnPointOverride.GenerateUnit(MetadataDatabase.Instance, UnityGameInstance.Instance.Game.DataManager, lanceOverride.selectedLanceDifficulty, lanceOverride.name, null, i, DataManager.Instance.GetSimGameCurrentDate(), companyTags);
        lanceOverride.unitSpawnPointOverrideList.Add(unitSpawnPointOverride);
      }
    }

    private void ApplyDifficultyMod(TeamOverride teamOverride, LanceOverride lanceOverride) {
      Faction faction = teamOverride.faction;
      int previousAjustedDifficulty = lanceOverride.lanceDifficultyAdjustment;
      int updatedLanceDifficultyAdjustment = Main.Settings.ExtendedLances.GetFactionLanceDifficulty(faction.ToString(), lanceOverride);
      
      if (previousAjustedDifficulty != updatedLanceDifficultyAdjustment) {
        Main.Logger.Log($"[AddExtraLanceSpawnPoints.ApplyDifficultyMod] [Faction:{teamOverride.faction}] Changing lance '{lanceOverride.name}' adjusted difficulty from '{lanceOverride.lanceDifficultyAdjustment}' to '{updatedLanceDifficultyAdjustment}'");
        lanceOverride.lanceDifficultyAdjustment = updatedLanceDifficultyAdjustment;
      }
    }
  }
}