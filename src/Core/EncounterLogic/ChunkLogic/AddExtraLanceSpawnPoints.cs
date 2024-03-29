using UnityEngine;

using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

using HBS.Collections;

using MissionControl.Data;
using MissionControl.Rules;
using MissionControl.Config;
using MissionControl.EncounterFactories;

using Harmony;

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

      // Backwards compatibility between EL v1 and v2 for renaming of key
      string targetKey = (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.ExtendedLances_TargetLanceSizeOverride)) ? ContractSettingsOverrides.ExtendedLances_TargetLanceSizeOverride : ContractSettingsOverrides.ExtendedLances_EnemyLanceSizeOverride;
      string employerKey = (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.ExtendedLances_EmployerLanceSizeOverride)) ? ContractSettingsOverrides.ExtendedLances_EmployerLanceSizeOverride : ContractSettingsOverrides.ExtendedLances_AllyLanceSizeOverride;

      PrepareIncreaseLanceMembers(contractOverride, contractOverride.targetTeam, targetKey, "Target");
      PrepareIncreaseLanceMembers(contractOverride, contractOverride.employerTeam, employerKey, "Employer");

      if (Main.Settings.ExtendedLances.EnableForTargetAlly) {
        PrepareIncreaseLanceMembers(contractOverride, contractOverride.targetsAllyTeam, ContractSettingsOverrides.ExtendedLances_TargetAllyLanceSizeOverride, "TargetAlly");
      } else {
        Main.LogDebug($"[AddExtraLanceSpawnPoints] [{contractOverride.targetsAllyTeam}] TargetAlly is 'false' so will not increase lance members");
      }

      if (Main.Settings.ExtendedLances.EnableForEmployerAlly) {
        PrepareIncreaseLanceMembers(contractOverride, contractOverride.employersAllyTeam, ContractSettingsOverrides.ExtendedLances_EmployerAllyLanceSizeOverride, "EmployerAlly");
      } else {
        Main.LogDebug($"[AddExtraLanceSpawnPoints] [{contractOverride.employersAllyTeam}] EmployerAlly is 'false' so will not increase lance members");
      }

      if (Main.Settings.ExtendedLances.EnableForHostileToAll) {
        PrepareIncreaseLanceMembers(contractOverride, contractOverride.hostileToAllTeam, ContractSettingsOverrides.ExtendedLances_HostileToAllLanceSizeOverride, "HostileToAll");
      } else {
        Main.LogDebug($"[AddExtraLanceSpawnPoints] [{contractOverride.hostileToAllTeam}] HostileToAll is 'false' so will not increase lance members");
      }

      if (Main.Settings.ExtendedLances.EnableForNeutralToAll) {
        PrepareIncreaseLanceMembers(contractOverride, contractOverride.neutralToAllTeam, ContractSettingsOverrides.ExtendedLances_NeutralToAllLanceSizeOverride, "NeutralToAll");
      } else {
        Main.LogDebug($"[AddExtraLanceSpawnPoints] [{contractOverride.neutralToAllTeam}] NeutralToAll is 'false' so will not increase lance members");
      }

      state.Set("ExtraLanceSpawnKeys", spawnKeys);
    }

    private void PrepareIncreaseLanceMembers(ContractOverride contractOverride, TeamOverride teamOverride, string FactionLanceSizeOverrideKey, string type) {
      Main.Logger.Log($"[AddExtraLanceSpawnPoints] [{type} - {teamOverride.faction}] Preparing to increase lance members for contract '{MissionControl.Instance.CurrentContract.Name}'.");
      IncreaseLanceSpawnPoints(contractOverride, teamOverride);
    }

    private bool CheckForLanceSkips(TeamOverride teamOverride, LanceOverride lanceOverride) {
      if (lancesToSkip.Contains(lanceOverride.GUID)) {
        Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected a lance to skip. Skipping.");
        return true;
      }

      if (lanceOverride.IsATurretLance()) {
        Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected a turret lance Ignoring for Extended Lances.");
        return true;
      }

      return false;
    }

    private void IncreaseLanceSpawnPoints(ContractOverride contractOverride, TeamOverride teamOverride) {
      List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;
      List<string> lancesToDelete = new List<string>();

      for (int i = 0; i < lanceOverrides.Count; i++) {
        LanceOverride lanceOverride = lanceOverrides[i];
        bool isManualLance = lanceOverride.lanceDefId == "Manual";

        if (lanceOverride.IsATurretLance()) {
          Main.Logger.LogDebug($"[AddExtraLanceSpawnPoints] Detected a turret lance. Skipping.");
          continue;
        }

        // At this point the number of units in a lance should be the expected amount.
        // The unitSpawnPointOverrides will be up to the EL limit set. They will be either:
        //  - Empty, Null, MechDef_None, Vehicle_None, Turret_None (Because they were a non-inheriting unit, manually entered as such or the LanceDef didn't have enough units to fill up the UnitOverride slots)
        //  - Filled resolved units (Because they were resolved by the LanceOverride LanceDef, or manually entered into the Contract Override)
        //  - 'Tagged', 'UseLance' or 'Manual' slots (Because they were not filled up by the LanceOverride LanceDef selection or Autofilled in Pass 1)
        int numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;

        if (CheckForLanceSkips(teamOverride, lanceOverride)) {
          Main.Logger.LogDebug($"[AddExtraLanceSpawnPoints] Detected a skip for this Lance. Skipping.");
          continue;
        }

        LanceSpawnerGameLogic lanceSpawner = lanceSpawners.Find(spawner => spawner.GUID == lanceOverride.lanceSpawner.EncounterObjectGuid);
        if (lanceSpawner != null) {
          AddSpawnPoints(lanceSpawner, teamOverride, lanceOverride, numberOfUnitsInLance);
        } else {
          Main.Logger.LogWarning($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Spawner is null for {lanceOverride.lanceSpawner.EncounterObjectGuid}. This is probably data from a restarted contract that hasn't been cleared up. It can be safely ignored.");
          lancesToDelete.Add(lanceOverride.lanceSpawner.EncounterObjectGuid);
        }


        if (Main.Settings.ExtendedLances.IsAutofillAllowed(contractOverride)) {
          // GUARD: If an AdditionalLance lance config has been set to 'supportAutoFill' false, then don't autofill
          if (lanceOverride is MLanceOverride) {
            MLanceOverride mLanceOverride = (MLanceOverride)lanceOverride;
            if (!mLanceOverride.SupportAutofill) {
              Main.LogDebug($"[AddExtraLanceSpawnPoints] Lance Override '{lanceOverride.name} - {mLanceOverride.GUID}' has 'autofill' explicitly turned off in MC lance '{mLanceOverride.LanceKey}'");
              continue;
            }
          }

          bool lanceOverrideForced = this.state.GetBool($"LANCE_OVERRIDE_FORCED_{lanceOverride.GUID}");
          bool lanceDefForced = this.state.GetBool($"LANCE_DEF_FORCED_{lanceOverride.GUID}");
          if (lanceOverrideForced || lanceDefForced) {
            Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected that lance '{lanceOverride.name} - {lanceOverride.GUID}' was forced using a LanceOverride or LanceDef EL enforcement. Skipping autofill.");
            continue;
          }

          List<GameObject> unitSpawnPoints = lanceSpawner.gameObject.FindAllContains("UnitSpawnPoint");

          if (unitSpawnPoints.Count <= 0) {
            Main.Logger.LogError($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Lance '{lanceOverride.name} - {lanceOverride.GUID}' has no UnitSpawnPoints. A Lance must have at least one unit spawn point to be valid");
          }

          List<string> unitSpawnPointGameLogicGUIDs = (List<string>)unitSpawnPoints.Select(unitSpawnPointGO => unitSpawnPointGO.GetComponent<UnitSpawnPointGameLogic>().GUID).ToList();

          // Only replace unresolved unit overrides if autofill is on
          int originalLanceOverrideSize = this.state.GetInt($"LANCE_ORIGINAL_UNIT_OVERRIDE_COUNT_{lanceOverride.GUID}");
          List<int> unresolvedIndexes = lanceOverride.GetUnresolvedUnitIndexes(originalLanceOverrideSize);
          Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Detected '{unresolvedIndexes.Count}' unresolved unit spawn overrides. Will resolve them before building spawn points.");
          if (unresolvedIndexes.Count > 0) {
            LanceDef loadedLanceDef = null;

            if (!isManualLance) {
              loadedLanceDef = (LanceDef)AccessTools.Field(typeof(LanceOverride), "loadedLanceDef").GetValue(lanceOverride);
              Main.LogDebug($"[AddExtraLanceSpawnPoints] [Faction:{teamOverride.faction}] Loaded LanceDef is '{loadedLanceDef.Description.Id}'");
            }

            ReplaceUnresolvedUnitOverride(unitSpawnPointGameLogicGUIDs, teamOverride, lanceOverride, loadedLanceDef, unresolvedIndexes);
          }

          // Fix any unmatching UnitSpawnPointOverrideGUIDs-to-UnitSpawnPointGameLogicGUIDs
          FixMisMatchedGUIDs(unitSpawnPointGameLogicGUIDs, teamOverride, lanceOverride);
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

    private void AddSpawnPoints(LanceSpawnerGameLogic lanceSpawner, TeamOverride teamOverride, LanceOverride lanceOverride, int numberOfUnitsInLance) {
      List<GameObject> unitSpawnPoints = lanceSpawner.gameObject.FindAllContains("UnitSpawnPoint");
      numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;

      if (unitSpawnPoints.Count <= 0) {
        Main.Logger.Log($"[AddSpawnPoints] Spawner '{lanceSpawner.name}' has '0' unit spawns containing the word 'UnitSpawnPoint'. A lance must have at least one valid spawn point. Skipping last '{lanceOverride.name}'");
        return;
      }

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
          unitSpawnPoints.Add(unitSpawnGameLogic.gameObject);

          string spawnKey = $"{spawnerName}.{unitSpawnGameLogic.gameObject.name}";
          encounterRules.ObjectLookup[spawnKey] = unitSpawnGameLogic.gameObject;
          spawnKeys.Add(new string[] { spawnKey, orientationKey });
        }
      }
    }

    private void ReplaceUnresolvedUnitOverride(List<string> unitSpawnPointGameLogicGUIDs, TeamOverride teamOverride, LanceOverride lanceOverride, LanceDef loadedLanceDef, List<int> unresolvedIndexes) {
      // Get all UnitSpawnPointGameLogic GUIDs
      List<string> availableUnitSpawnPointGameLogicGUIDsList = new List<string>(unitSpawnPointGameLogicGUIDs);
      Stack<string> availableUnitSpawnPointGameLogicGUIDsStack;

      // Ensure no duplicates exist. If so, modder error in their set up.
      if (unitSpawnPointGameLogicGUIDs.Distinct().ToList().Count != unitSpawnPointGameLogicGUIDs.Count()) {
        Main.Logger.LogError($"[AddExtraLanceSpawnPoints.ReplaceUnresolvedUnitOverride] [Faction:{teamOverride.faction}] The lance '{lanceOverride.GUID}' has duplicate UnitSpawnPointOverride GUIDs. This is incorrect and will cause some units not to spawn! This is not a problem with MC. Please fix this in the contract override data!");
      }

      // Remove any available UnitSpawnPointGameLogicGUIDs that are correctly set in the UnitSpawnPointOverrides
      // The remaining GUIDs are from unassigned UnitSpawnPointGameLogics and they can be assigned below
      foreach (UnitSpawnPointOverride unitSpawnPointOverride in lanceOverride.unitSpawnPointOverrideList) {
        if (availableUnitSpawnPointGameLogicGUIDsList.Contains(unitSpawnPointOverride.GUID)) {
          availableUnitSpawnPointGameLogicGUIDsList.Remove(unitSpawnPointOverride.GUID);
        }
      }

      Main.Logger.LogDebug($"[AddExtraLanceSpawnPoints.ReplaceUnresolvedUnitOverride] [Faction:{teamOverride.faction}] AvailableUnitSpawnPointGameLogicGUIDs will contain Game Logic GUID: '{string.Join(", ", unitSpawnPointGameLogicGUIDs)}'");
      availableUnitSpawnPointGameLogicGUIDsStack = new Stack<string>(availableUnitSpawnPointGameLogicGUIDsList);

      foreach (int index in unresolvedIndexes) {
        UnitSpawnPointOverride originalUnitSpawnPointOverride = lanceOverride.GetUnitToCopy();
        UnitSpawnPointOverride unitSpawnPointOverride = originalUnitSpawnPointOverride.DeepCopy();

        if (unitSpawnPointGameLogicGUIDs.Count < index) {
          Main.Logger.LogError($"[AddExtraLanceSpawnPoints.ReplaceUnresolvedUnitOverride] [Faction:{teamOverride.faction}] There are more unit overrides in lance '{lanceOverride.name} - {lanceOverride.GUID}' than unit spawn points in LanceSpawner. This should never happen.");
        }

        string indexUnitSpawnPointOverrideGUID = lanceOverride.unitSpawnPointOverrideList[index].GUID;
        Main.Logger.LogDebug($"[AddExtraLanceSpawnPoints.ReplaceUnresolvedUnitOverride] [Faction:{teamOverride.faction}] [Unit{index + 1}] Unit GUID is '{indexUnitSpawnPointOverrideGUID}'");
        if (unitSpawnPointGameLogicGUIDs.Contains(indexUnitSpawnPointOverrideGUID)) {
          unitSpawnPointOverride.unitSpawnPoint.EncounterObjectGuid = indexUnitSpawnPointOverrideGUID; // If force resolving - then ensure GUIDs for spawner unit spawns are maintained
        } else {
          if (availableUnitSpawnPointGameLogicGUIDsStack.Count > 0) {
            string guid = availableUnitSpawnPointGameLogicGUIDsStack.Pop();
            Main.Logger.LogDebug($"[AddExtraLanceSpawnPoints.ReplaceUnresolvedUnitOverride] [Faction:{teamOverride.faction}] [Unit{index + 1}] GUID '{unitSpawnPointOverride.unitSpawnPoint.EncounterObjectGuid}' does not exist for a UnitSpawnerGameLogic object. Reassigning with GUID '{guid}'");
            unitSpawnPointOverride.unitSpawnPoint.EncounterObjectGuid = guid; // If force resolving - then ensure GUIDs for spawner unit spawns are maintained
          } else {
            Main.Logger.LogError($"[AddExtraLanceSpawnPoints.ReplaceUnresolvedUnitOverride] [Faction:{teamOverride.faction}] [Unit{index + 1}] No avaiable spare unit spawner guids to assign to unit override. This should never happen!");
          }
        }

        unitSpawnPointOverride.customUnitName = "";
        TagSet companyTags = new TagSet(UnityGameInstance.BattleTechGame.Simulation.CompanyTags);

        Main.LogDebug($"[AddExtraLanceSpawnPoints.ReplaceUnresolvedUnitOverride] Generating unresolved unit '{index + 1}'");
        unitSpawnPointOverride.GenerateUnit(MetadataDatabase.Instance, UnityGameInstance.Instance.Game.DataManager, lanceOverride.selectedLanceDifficulty, lanceOverride.name, loadedLanceDef == null ? null : loadedLanceDef.Description.Id, index, DataManager.Instance.GetSimGameCurrentDate(), companyTags);
        lanceOverride.unitSpawnPointOverrideList[index] = unitSpawnPointOverride;
      }
    }

    private void FixMisMatchedGUIDs(List<string> unitSpawnPointGameLogicGUIDs, TeamOverride teamOverride, LanceOverride lanceOverride) {
      // Get all UnitSpawnPointGameLogic GUIDs
      List<string> availableUnitSpawnPointGameLogicGUIDsList = new List<string>(unitSpawnPointGameLogicGUIDs);
      Stack<string> availableUnitSpawnPointGameLogicGUIDsStack;

      // Ensure no duplicates exist. If so, modder error in their set up.
      if (unitSpawnPointGameLogicGUIDs.Distinct().ToList().Count != unitSpawnPointGameLogicGUIDs.Count()) {
        Main.Logger.LogError($"[AddExtraLanceSpawnPoints.FixMisMatchedGUIDs] [Faction:{teamOverride.faction}] The lance '{lanceOverride.GUID}' has duplicate UnitSpawnPointOverride GUIDs. This is incorrect and will cause some units not to spawn! This is not a problem with MC. Please fix this in the contract override data!");
      }

      // Remove any available UnitSpawnPointGameLogicGUIDs that are correctly set in the UnitSpawnPointOverrides
      // The remaining GUIDs are from unassigned UnitSpawnPointGameLogics and they can be assigned below
      foreach (UnitSpawnPointOverride unitSpawnPointOverride in lanceOverride.unitSpawnPointOverrideList) {
        if (availableUnitSpawnPointGameLogicGUIDsList.Contains(unitSpawnPointOverride.GUID)) {
          availableUnitSpawnPointGameLogicGUIDsList.Remove(unitSpawnPointOverride.GUID);
        }
      }

      Main.Logger.LogDebug($"[AddExtraLanceSpawnPoints.FixMisMatchedGUIDs] [Faction:{teamOverride.faction}] AvailableUnitSpawnPointGameLogicGUIDs will contain Game Logic GUID: '{string.Join(", ", unitSpawnPointGameLogicGUIDs)}'");
      availableUnitSpawnPointGameLogicGUIDsStack = new Stack<string>(availableUnitSpawnPointGameLogicGUIDsList);

      for (int i = 0; i < lanceOverride.unitSpawnPointOverrideList.Count; i++) {
        UnitSpawnPointOverride unitSpawnPointOverride = lanceOverride.unitSpawnPointOverrideList[i];
        string unitSpawnPointOverrideGUID = unitSpawnPointOverride.GUID;

        if (!unitSpawnPointGameLogicGUIDs.Contains(unitSpawnPointOverrideGUID)) {
          if (availableUnitSpawnPointGameLogicGUIDsStack.Count > 0) {
            string guid = availableUnitSpawnPointGameLogicGUIDsStack.Pop();
            Main.Logger.LogDebug($"[AddExtraLanceSpawnPoints.FixMisMatchedGUIDs] [Faction:{teamOverride.faction}] [Unit{i + 1}] GUID '{unitSpawnPointOverride.unitSpawnPoint.EncounterObjectGuid}' does not exist for a UnitSpawnerGameLogic object. Reassigning with GUID '{guid}'");
            unitSpawnPointOverride.unitSpawnPoint.EncounterObjectGuid = guid; // If force resolving - then ensure GUIDs for spawner unit spawns are maintained
          } else {
            Main.Logger.LogError($"[AddExtraLanceSpawnPoints.FixMisMatchedGUIDs] [Faction:{teamOverride.faction}] [Unit{i + 1}] No available spare unit spawner guids to assign to unit override. This should never happen!");
          }
        }
      }
    }
  }
}