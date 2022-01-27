using System;
using System.Collections.Generic;

using BattleTech.Framework;

using HBS.Collections;

using MissionControl.Data;
using MissionControl.Config;

namespace MissionControl.Logic {
  public class AddExtraLanceMembers : LanceLogic {
    private LogicState state;
    private List<string> lancesToSkip = new List<string>();

    public AddExtraLanceMembers(LogicState state) {
      this.state = state;
      this.state.Set("LancesToSkip", lancesToSkip);
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddExtraLanceMembers] Adding extra lance units to lance");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;

      // Backwards compatibility between EL v1 and v2 for renaming of key
      string targetKey = (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.ExtendedLances_TargetLanceSizeOverride)) ? ContractSettingsOverrides.ExtendedLances_TargetLanceSizeOverride : ContractSettingsOverrides.ExtendedLances_EnemyLanceSizeOverride;
      string employerKey = (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.ExtendedLances_EmployerLanceSizeOverride)) ? ContractSettingsOverrides.ExtendedLances_EmployerLanceSizeOverride : ContractSettingsOverrides.ExtendedLances_AllyLanceSizeOverride;

      PrepareIncreaseLanceMembers(contractOverride, contractOverride.targetTeam, targetKey, "Target");
      PrepareIncreaseLanceMembers(contractOverride, contractOverride.employerTeam, employerKey, "Employer");

      if (Main.Settings.ExtendedLances.EnableForTargetAlly) PrepareIncreaseLanceMembers(contractOverride, contractOverride.targetsAllyTeam, ContractSettingsOverrides.ExtendedLances_TargetAllyLanceSizeOverride, "TargetAlly");
      if (Main.Settings.ExtendedLances.EnableForEmployerAlly) PrepareIncreaseLanceMembers(contractOverride, contractOverride.employersAllyTeam, ContractSettingsOverrides.ExtendedLances_EmployerAllyLanceSizeOverride, "EmployerAlly");
      if (Main.Settings.ExtendedLances.EnableForHostileToAll) PrepareIncreaseLanceMembers(contractOverride, contractOverride.hostileToAllTeam, ContractSettingsOverrides.ExtendedLances_HostileToAllLanceSizeOverride, "HostileToAll");
      if (Main.Settings.ExtendedLances.EnableForNeutralToAll) PrepareIncreaseLanceMembers(contractOverride, contractOverride.neutralToAllTeam, ContractSettingsOverrides.ExtendedLances_NeutralToAllLanceSizeOverride, "NeutralToAll");
    }

    private void PrepareIncreaseLanceMembers(ContractOverride contractOverride, TeamOverride teamOverride, string FactionLanceSizeOverrideKey, string type) {
      if (Main.Settings.ActiveContractSettings.Has(FactionLanceSizeOverrideKey)) {
        int lanceSizeOverride = Main.Settings.ActiveContractSettings.GetInt(FactionLanceSizeOverrideKey);
        Main.Logger.Log($"[AddExtraLanceMembers] [{teamOverride.faction}] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. {type} lance size will be '{lanceSizeOverride}'.");
        IncreaseLanceMembers(contractOverride, teamOverride, lanceSizeOverride);
      } else {
        IncreaseLanceMembers(contractOverride, teamOverride);
      }
    }

    private bool CheckForLanceOverrideSkips(TeamOverride teamOverride, LanceOverride lanceOverride) {
      Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Checking lance '{lanceOverride.name}'...");
      if (Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAny().Count > 0 && lanceOverride.lanceTagSet.ContainsAny(Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAny())) {
        Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Lance contains a tag set in 'SkipWhenTaggedWithAny'. Skipping '{lanceOverride.name}'");
        lancesToSkip.Add(lanceOverride.GUID);
        return true;
      }

      if (Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAll().Count > 0 && lanceOverride.lanceTagSet.ContainsAll(Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAll())) {
        Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Lance contains a tag set in 'SkipWhenTaggedWithAll'. Skipping '{lanceOverride.name}'");
        lancesToSkip.Add(lanceOverride.GUID);
        return true;
      }

      if (Main.Settings.ExtendedLances.GetSkipWhenExcludeTagsContain().Count > 0 && lanceOverride.lanceExcludedTagSet.ContainsAny(Main.Settings.ExtendedLances.GetSkipWhenExcludeTagsContain())) {
        Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Lance contains an exclude tag set in 'GetSkipWhenExcludeTagsContain'. Skipping '{lanceOverride.name}'");
        lancesToSkip.Add(lanceOverride.GUID);
        return true;
      }

      return false;
    }

    // Checks if the LanceOverride lance unit count should be used to override the Faction lance unit count
    // It does this by checking a set tag. If it's present then it will be forced.
    private bool IsLanceOverrideForced(LanceOverride lanceOverride) {
      if (lanceOverride.lanceTagSet.GetTagSetSourceFile().Contains(Main.Settings.ExtendedLances.ForceLanceOverrideSizeWithTag)) return true;
      return false;
    }

    private void IncreaseLanceMembers(ContractOverride contractOverride, TeamOverride teamOverride, int lanceSizeOverride = -1) {
      List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;
      int factionLanceSize = lanceSizeOverride <= -1 ? Main.Settings.ExtendedLances.GetFactionLanceSize(teamOverride.faction.ToString()) : lanceSizeOverride;
      Main.LogDebug($"[AddExtraLanceMembers] Faction '{teamOverride.faction}' lance size is '{factionLanceSize}'");

      foreach (LanceOverride lanceOverride in lanceOverrides) {
        int intendedLanceSize = factionLanceSize;
        int numberOfUnitsInLanceOverride = lanceOverride.unitSpawnPointOverrideList.Count;
        bool mLanceOverrideSkipAutofill = false;

        if (lanceOverride is MLanceOverride) {
          MLanceOverride mLanceOverride = (MLanceOverride)lanceOverride;
          if (!mLanceOverride.SupportAutofill) Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] [Lance {lanceOverride.GUID}] Lance is an MC defined Lance. It is defined to not support autofilling.");
          mLanceOverrideSkipAutofill = !mLanceOverride.SupportAutofill;
        }

        // Store original LanceOverride unitOverride count for later logic
        Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] [Lance {lanceOverride.GUID}] Saving original LanceOverride UnitOverrideList count of '{numberOfUnitsInLanceOverride}'");
        this.state.Set($"LANCE_ORIGINAL_UNIT_OVERRIDE_COUNT_{lanceOverride.GUID}", numberOfUnitsInLanceOverride);

        // If any lance should be skipped, check and skip
        if (CheckForLanceOverrideSkips(teamOverride, lanceOverride)) continue;

        if (IsLanceOverrideForced(lanceOverride)) {
          Main.LogDebug($"[AddExtraLanceMembers] Force overriding lance override '{lanceOverride.name}' from faction size of '{intendedLanceSize}' to '{numberOfUnitsInLanceOverride}'");
          this.state.Set($"LANCE_OVERRIDE_FORCED_{lanceOverride.GUID}", true);
          intendedLanceSize = numberOfUnitsInLanceOverride;
        }

        ApplyDifficultyMod(teamOverride, lanceOverride);

        // To support LanceDefs that have more than four units in them we need to populate the ContractOverride to contain empty unitSpawnPointOverrides
        // If the lance is: 'Manual', 'Tagged', 'UseLance', or a direct lance reference
        //    - If 'Autofill' is off: we populate with empty slots but we leave the actual resolved filling for later in the life cycle (AddExtraLanceSpawnPoints handles it).
        //    - If 'Autofill' is on: we make copies of any of the tagged unitSpawnPointOverrides in the lance to fill up the slots required
        if (numberOfUnitsInLanceOverride < intendedLanceSize) {
          if (Main.Settings.ExtendedLances.Autofill && !mLanceOverrideSkipAutofill) {
            AutofillWithTaggedOrFirstUnitOverrideSlots(teamOverride, lanceOverride, numberOfUnitsInLanceOverride, intendedLanceSize);
          } else {
            AutofillWithEmptyUnitOverrideSlots(teamOverride, lanceOverride, numberOfUnitsInLanceOverride, intendedLanceSize);
          }
        }
      }
    }

    private void AutofillWithTaggedOrFirstUnitOverrideSlots(TeamOverride teamOverride, LanceOverride lanceOverride, int numberOfUnitsInLanceOverride, int intendedLanceSize) {
      for (int i = numberOfUnitsInLanceOverride; i < intendedLanceSize; i++) {
        UnitSpawnPointOverride originalUnitSpawnPointOverride = lanceOverride.GetAnyTaggedLanceMember();

        if (originalUnitSpawnPointOverride == null) {
          originalUnitSpawnPointOverride = lanceOverride.unitSpawnPointOverrideList[0];
          Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Autofill mode. Adding unit {i + 1} by duplicating the first unit of the lance.");
        } else {
          Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Autofill mode. Adding unit {i + 1} by duplicating a 'Tagged' or 'Use Lance' lance member.");
        }

        UnitSpawnPointOverride unitSpawnPointOverride = originalUnitSpawnPointOverride.DeepCopy();
        unitSpawnPointOverride.customUnitName = "";
        Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Using unit with old GUID '{originalUnitSpawnPointOverride.GUID}' and new GUID '{unitSpawnPointOverride.GUID}' unitDefId '{unitSpawnPointOverride.unitDefId}' as a base");

        lanceOverride.unitSpawnPointOverrideList.Add(unitSpawnPointOverride);
      }
    }

    private void AutofillWithEmptyUnitOverrideSlots(TeamOverride teamOverride, LanceOverride lanceOverride, int numberOfUnitsInLanceOverride, int intendedLanceSize) {
      UnitSpawnPointOverride emptyUnitSpawnPointOverride = new UnitSpawnPointOverride();

      for (int i = numberOfUnitsInLanceOverride; i < intendedLanceSize; i++) {
        Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Non-autofill mode. Expanding lance size for position {i + 1} with a placeholder empty unit override.");
        lanceOverride.unitSpawnPointOverrideList.Add(emptyUnitSpawnPointOverride.DeepCopy());
      }
    }

    private bool AreAnyLanceMembersTagged(LanceOverride lanceOverride) {
      UnitSpawnPointOverride unitSpawnOverrides = lanceOverride.GetAnyTaggedLanceMember();
      return (unitSpawnOverrides != null);
    }

    private void ApplyDifficultyMod(TeamOverride teamOverride, LanceOverride lanceOverride) {
      int previousAjustedDifficulty = lanceOverride.lanceDifficultyAdjustment;
      int updatedLanceDifficultyAdjustment = Main.Settings.ExtendedLances.GetFactionLanceDifficulty(teamOverride.faction, lanceOverride);

      if (previousAjustedDifficulty != updatedLanceDifficultyAdjustment) {
        Main.Logger.Log($"[AddExtraLanceMembers.ApplyDifficultyMod] [Faction:{teamOverride.faction}] Changing lance '{lanceOverride.name}' adjusted difficulty from '{lanceOverride.lanceDifficultyAdjustment}' to '{updatedLanceDifficultyAdjustment}'");
        lanceOverride.lanceDifficultyAdjustment = updatedLanceDifficultyAdjustment;
      }
    }
  }
}