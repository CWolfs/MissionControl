using UnityEngine;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

using HBS.Collections;

using MissionControl.Data;
using MissionControl.Rules;
using MissionControl.Config;
using MissionControl.EncounterFactories;

namespace MissionControl.Logic {
  public class AddExtraLanceSpawnPoints : ChunkLogic {
    private EncounterRules encounterRules;
    private LogicState state;
    private List<LanceSpawnerGameLogic> lanceSpawners;
    private List<string[]> spawnKeys = new List<string[]>();
    private List<string> lancesToSkip = new List<string>();

    public AddExtraLanceSpawnPoints(EncounterRules encounterRules, LogicState state) {
      this.encounterRules = encounterRules;
      this.state = state;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddExtraLanceSpawnPoints] Adding lance spawn points to match contract override data");
      Contract contract = MissionControl.Instance.CurrentContract;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      ContractOverride contractOverride = contract.Override;

      lancesToSkip = (List<string>)state.GetObject("LancesToSkip");
      lanceSpawners = new List<LanceSpawnerGameLogic>(encounterLayerData.gameObject.GetComponentsInChildren<LanceSpawnerGameLogic>());

      TeamOverride targetTeamOverride = contractOverride.targetTeam;
      TeamOverride employerTeamOverride = contractOverride.employerTeam;

      if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.ExtendedLances_EnemyLanceSizeOverride)) {
        int lanceSizeOverride = Main.Settings.ActiveContractSettings.GetInt(ContractSettingsOverrides.ExtendedLances_EnemyLanceSizeOverride);
        Main.Logger.Log($"[AddExtraLanceSpawnPoints] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Enemy lance size will be '{lanceSizeOverride}'.");
        IncreaseLanceSpawnPoints(contractOverride, targetTeamOverride, lanceSizeOverride);
      } else {
        IncreaseLanceSpawnPoints(contractOverride, targetTeamOverride);
      }

      if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.ExtendedLances_AllyLanceSizeOverride)) {
        int lanceSizeOverride = Main.Settings.ActiveContractSettings.GetInt(ContractSettingsOverrides.ExtendedLances_AllyLanceSizeOverride);
        Main.Logger.Log($"[AddExtraLanceSpawnPoints] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Ally lance size will be '{lanceSizeOverride}'.");
        IncreaseLanceSpawnPoints(contractOverride, employerTeamOverride, lanceSizeOverride);
      } else {
        IncreaseLanceSpawnPoints(contractOverride, employerTeamOverride);
      }

      state.Set("ExtraLanceSpawnKeys", spawnKeys);
    }

    private void IncreaseLanceSpawnPoints(ContractOverride contractOverride, TeamOverride teamOverride, int lanceSizeOverride = -1) {
      List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;
      int factionLanceSize = lanceSizeOverride <= -1 ? Main.Settings.ExtendedLances.GetFactionLanceSize(teamOverride.faction.ToString()) : lanceSizeOverride;
      List<string> lancesToDelete = new List<string>();

      for (int i = 0; i < lanceOverrides.Count; i++) {
        LanceOverride lanceOverride = lanceOverrides[i];
        bool isManualLance = lanceOverride.lanceDefId == "Manual";
        // At this point the number of units in a lance should be the expected amount.
        // The unitSpawnPointOverrides will be up to the EL limit set. They will be either:
        //  - Empty
        //  - Filled resolved units
        //  - 'Tagged', 'UseLance' or 'Manual' slots
        int numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;

        if (lancesToSkip.Contains(lanceOverride.GUID)) {
          Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected a lance to skip. Skipping.");
          continue;
        }

        if (lanceOverride.IsATurretLance()) {
          Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected a turret lance Ignoring for Extended Lances.");
          continue;
        }

        if (isManualLance && numberOfUnitsInLance <= 0) {
          Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected a lance that is set to manual but no units were manually specified. This is a bad contract json setup. Fix it! Ignoring for Extended Lances");
          continue;
        }

        if (Main.Settings.ExtendedLances.Autofill) {
          // GUARD: If an AdditionalLance lance config has been set to 'supportAutoFill' false, then don't autofill
          if (lanceOverride is MLanceOverride) {
            MLanceOverride mLanceOverride = (MLanceOverride)lanceOverride;
            if (!mLanceOverride.SupportAutofill) {
              Main.LogDebug($"[AddExtraLanceSpawnPoints] Lance Override '{mLanceOverride.GUID}' has 'autofill' explicitly turned off in MC lance '{mLanceOverride.LanceKey}'");
              continue;
            }
          }

          List<int> unresolvedIndexes = lanceOverride.GetUnresolvedUnitIndexes();
          Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected '{unresolvedIndexes.Count} unresolved unit spawn overrides. Will resolve them before building spawn points.'");
          foreach (int index in unresolvedIndexes) {
            ReplaceUnresolvedUnitOverride(lanceOverride, index);
          }
        } else {
          Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Populated lance '{lanceOverride.name}' has fewer units than the faction requires. Allowing as a valid setup as 'Autofill' is false");
        }

        // if ((numberOfUnitsInLance < factionLanceSize) && numberOfUnitsInLance > 0) {
        //   // This is usually from a 'tagged' lance being selected which has less lance members than the faction lance size
        //   if (Main.Settings.ExtendedLances.Autofill) {
        //     Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Populated lance '{lanceOverride.name}' has fewer units than the faction requires. Autofilling the missing units");

        //     // GUARD: If an AdditionalLance lance config has been set to 'supportAutoFill' false, then don't autofill
        //     if (lanceOverride is MLanceOverride) {
        //       MLanceOverride mLanceOverride = (MLanceOverride)lanceOverride;
        //       if (!mLanceOverride.SupportAutofill) {
        //         Main.LogDebug($"[AddExtraLanceSpawnPoints] Lance Override '{mLanceOverride.GUID}' has 'autofill' explicitly turned off in MC lance '{mLanceOverride.LanceKey}'");
        //         continue;
        //       }
        //     }

        //     AddNewLanceMembers(contractOverride, teamOverride, lanceOverride, numberOfUnitsInLance, factionLanceSize);
        //   } else {
        //     Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Populated lance '{lanceOverride.name}' has fewer units than the faction requires. Allowing as a valid setup as 'Autofill' is false");
        //   }
        // }

        LanceSpawnerGameLogic lanceSpawner = lanceSpawners.Find(spawner => spawner.GUID == lanceOverride.lanceSpawner.EncounterObjectGuid);
        if (lanceSpawner != null) {
          List<GameObject> unitSpawnPoints = lanceSpawner.gameObject.FindAllContains("UnitSpawnPoint");
          numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;

          if (numberOfUnitsInLance > unitSpawnPoints.Count) {
            Main.Logger.Log($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected lance '{lanceOverride.name}' has more units than lance spawn points. Creating new lance spawns to accommodate.");
            string spawnerName = lanceSpawner.gameObject.name;
            GameObject orientationUnit = unitSpawnPoints[0].gameObject;
            string orientationKey = $"{spawnerName}.{orientationUnit.name}";
            encounterRules.ObjectLookup[orientationKey] = orientationUnit;

            for (int j = unitSpawnPoints.Count; j < numberOfUnitsInLance; j++) {
              Vector3 randomLanceSpawn = unitSpawnPoints.GetRandom().transform.localPosition;
              Vector3 spawnPositon = SceneUtils.GetRandomPositionFromTarget(randomLanceSpawn, 24, 100);
              spawnPositon = spawnPositon.GetClosestHexLerpedPointOnGrid();

              // Ensure spawn position isn't on another unit spawn. Give up if one isn't possible.
              int failSafe = 0;
              while (spawnPositon.IsTooCloseToAnotherSpawn()) {
                spawnPositon = SceneUtils.GetRandomPositionFromTarget(randomLanceSpawn, 24, 100);
                spawnPositon = spawnPositon.GetClosestHexLerpedPointOnGrid();
                if (failSafe > 20) break;
                failSafe++;
              }

              Main.Logger.Log($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Creating lance '{lanceOverride.name}' spawn point 'UnitSpawnPoint{j + 1}'");
              UnitSpawnPointGameLogic unitSpawnGameLogic = LanceSpawnerFactory.CreateUnitSpawnPoint(lanceSpawner.gameObject, $"UnitSpawnPoint{j + 1}", spawnPositon, lanceOverride.unitSpawnPointOverrideList[j].unitSpawnPoint.EncounterObjectGuid);

              string spawnKey = $"{spawnerName}.{unitSpawnGameLogic.gameObject.name}";
              encounterRules.ObjectLookup[spawnKey] = unitSpawnGameLogic.gameObject;
              spawnKeys.Add(new string[] { spawnKey, orientationKey });
            }
          }
        } else {
          Main.Logger.LogWarning($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Spawner is null for {lanceOverride.lanceSpawner.EncounterObjectGuid}. This is probably data from a restarted contract that hasn't been cleared up. It can be safely ignored.");
          lancesToDelete.Add(lanceOverride.lanceSpawner.EncounterObjectGuid);
        }
      }

      for (int i = (lanceOverrides.Count - 1); i >= 0; i--) {
        LanceOverride lanceOverride = lanceOverrides[i];

        foreach (string lanceToDeleteByGuid in lancesToDelete) {
          if (lanceOverride.lanceSpawner.EncounterObjectGuid == lanceToDeleteByGuid) {
            Main.Logger.LogWarning($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Removing old lance data from contract. Deleting lance '{lanceToDeleteByGuid}'");
            lanceOverrides.Remove(lanceOverride);
          }
        }
      }
    }

    private void ReplaceUnresolvedUnitOverride(LanceOverride lanceOverride, int index) {
      UnitSpawnPointOverride originalUnitSpawnPointOverride = lanceOverride.GetAnyTaggedLanceMember();
      if (originalUnitSpawnPointOverride == null) originalUnitSpawnPointOverride = lanceOverride.unitSpawnPointOverrideList[0];
      UnitSpawnPointOverride unitSpawnPointOverride = originalUnitSpawnPointOverride.DeepCopy();
      unitSpawnPointOverride.customUnitName = "";
      TagSet companyTags = new TagSet(UnityGameInstance.BattleTechGame.Simulation.CompanyTags);

      unitSpawnPointOverride.GenerateUnit(MetadataDatabase.Instance, UnityGameInstance.Instance.Game.DataManager, lanceOverride.selectedLanceDifficulty, lanceOverride.name, null, index, DataManager.Instance.GetSimGameCurrentDate(), companyTags);
      lanceOverride.unitSpawnPointOverrideList[index] = unitSpawnPointOverride;
    }

    // private void AddNewLanceMembers(ContractOverride contractOverride, TeamOverride teamOverride, LanceOverride lanceOverride, int numberOfUnitsInLance, int factionLanceSize) {
    //   for (int i = numberOfUnitsInLance; i < factionLanceSize; i++) {
    //     UnitSpawnPointOverride originalUnitSpawnPointOverride = lanceOverride.GetAnyTaggedLanceMember();
    //     if (originalUnitSpawnPointOverride == null) originalUnitSpawnPointOverride = lanceOverride.unitSpawnPointOverrideList[0];
    //     UnitSpawnPointOverride unitSpawnPointOverride = originalUnitSpawnPointOverride.DeepCopy();
    //     unitSpawnPointOverride.customUnitName = "";
    //     TagSet companyTags = new TagSet(UnityGameInstance.BattleTechGame.Simulation.CompanyTags);

    //     unitSpawnPointOverride.GenerateUnit(MetadataDatabase.Instance, UnityGameInstance.Instance.Game.DataManager, lanceOverride.selectedLanceDifficulty, lanceOverride.name, null, i, DataManager.Instance.GetSimGameCurrentDate(), companyTags);
    //     lanceOverride.unitSpawnPointOverrideList.Add(unitSpawnPointOverride);
    //   }
    // }
  }
}