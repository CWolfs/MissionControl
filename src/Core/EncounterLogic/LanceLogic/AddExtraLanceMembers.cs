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

      TeamOverride targetTeamOverride = contractOverride.targetTeam;
      TeamOverride employerTeamOverride = contractOverride.employerTeam;

      if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.ExtendedLances_EnemyLanceSizeOverride)) {
        int lanceSizeOverride = Main.Settings.ActiveContractSettings.GetInt(ContractSettingsOverrides.ExtendedLances_EnemyLanceSizeOverride);
        Main.Logger.Log($"[AddExtraLanceMembers] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Enemy lance size will be '{lanceSizeOverride}'.");
        IncreaseLanceMembers(contractOverride, targetTeamOverride, lanceSizeOverride);
      } else {
        IncreaseLanceMembers(contractOverride, targetTeamOverride);
      }

      if (Main.Settings.ActiveContractSettings.Has(ContractSettingsOverrides.ExtendedLances_AllyLanceSizeOverride)) {
        int lanceSizeOverride = Main.Settings.ActiveContractSettings.GetInt(ContractSettingsOverrides.ExtendedLances_AllyLanceSizeOverride);
        Main.Logger.Log($"[AddExtraLanceMembers] Using contract-specific settings override for contract '{MissionControl.Instance.CurrentContract.Name}'. Ally lance size will be '{lanceSizeOverride}'.");
        IncreaseLanceMembers(contractOverride, employerTeamOverride, lanceSizeOverride);
      } else {
        IncreaseLanceMembers(contractOverride, employerTeamOverride);
      }
    }

    private void IncreaseLanceMembers(ContractOverride contractOverride, TeamOverride teamOverride, int lanceSizeOverride = -1) {
      List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;
      int factionLanceSize = lanceSizeOverride <= -1 ? Main.Settings.ExtendedLances.GetFactionLanceSize(teamOverride.faction.ToString()) : lanceSizeOverride;
      Main.LogDebug($"[AddExtraLanceMembers] Faction '{teamOverride.faction}' lance size is '{factionLanceSize}");

      foreach (LanceOverride lanceOverride in lanceOverrides) {
        Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Checking lance '{lanceOverride.name}'...");
        if (Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAny().Count > 0 && lanceOverride.lanceTagSet.ContainsAny(Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAny())) {
          Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Lance contains a tag set in 'SkipWhenTaggedWithAny'. Skipping '{lanceOverride.name}'");
          lancesToSkip.Add(lanceOverride.GUID);
          continue;
        }

        if (Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAll().Count > 0 && lanceOverride.lanceTagSet.ContainsAll(Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAll())) {
          Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Lance contains a tag set in 'SkipWhenTaggedWithAll'. Skipping '{lanceOverride.name}'");
          lancesToSkip.Add(lanceOverride.GUID);
          continue;
        }

        if (Main.Settings.ExtendedLances.GetSkipWhenExcludeTagsContain().Count > 0 && lanceOverride.lanceExcludedTagSet.ContainsAny(Main.Settings.ExtendedLances.GetSkipWhenExcludeTagsContain())) {
          Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Lance contains an exclude tag set in 'GetSkipWhenExcludeTagsContain'. Skipping '{lanceOverride.name}'");
          lancesToSkip.Add(lanceOverride.GUID);
          continue;
        }

        // GUARD: If an AdditionalLance lance config has been set to 'supportAutofill' false, then don't autofill
        if (lanceOverride is MLanceOverride) {
          MLanceOverride mLanceOverride = (MLanceOverride)lanceOverride;
          if (!mLanceOverride.SupportAutofill) {
            Main.LogDebug($"[AddExtraLanceMembers] LanceOverride '{mLanceOverride.GUID}' has 'supportAutofill' explicitly set to 'false' in MC lance '{mLanceOverride.LanceKey}'. Will not autofill.");
            lancesToSkip.Add(mLanceOverride.GUID);
            continue;
          }
        }

        int numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;
        // bool isLanceTagged = lanceOverride.lanceDefId == "Tagged" || lanceOverride.lanceDefId == "UseLance";
        bool isManualLance = lanceOverride.lanceDefId == "Manual";
        bool AreAnyLanceUnitsTagged = AreAnyLanceMembersTagged(lanceOverride);

        ApplyDifficultyMod(teamOverride, lanceOverride);

        // To support LanceDefs that have more than four units in them we need to populate the ContractOverride to contain empty unitSpawnPointOverrides
        // If the lance is: 'Tagged', 'UseLance', or a direct lance reference
        //    - If 'Autofill' is off: we populate with empty slots but we leave the actual resolved filling for later in the life cycle (AddExtraLanceSpawnPoints handles it).
        //    - If 'Autofill' is on: we make copies of any of the tagged unitSpawnPointOverrides in the lance to fill up the slots required
        // If the lance is: 'Manual': we inform the modder they are meant to add the correct slot count in the contract json
        if (numberOfUnitsInLance < factionLanceSize) {
          if (isManualLance) {
            Main.Logger.LogWarning($"[AddExtraLanceMembers] [{teamOverride.faction}] The contract '{contractOverride.ID}' for the team '{teamOverride.teamName}' has a manual contract-defined lance and manual unit setup but it does not specify the right number of lance members. When manually setting lances they should match the Mission Control ExtendedLance lance member count. For this lance you should probably have exactly '{factionLanceSize}' but only '{numberOfUnitsInLance}' are set.");
            continue;
          }

          if (Main.Settings.ExtendedLances.Autofill) {
            for (int i = numberOfUnitsInLance; i < factionLanceSize; i++) {
              UnitSpawnPointOverride originalUnitSpawnPointOverride = lanceOverride.GetAnyTaggedLanceMember();
              if (originalUnitSpawnPointOverride == null) {
                originalUnitSpawnPointOverride = lanceOverride.unitSpawnPointOverrideList[0];
                Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Autofill mode. Adding unit {i + 1} by duplicating the first unit of the lance.");
              } else {
                Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Autofill mode. Adding unit {i + 1} by duplicating a 'Tagged' or 'Use Lance' lance member.");
              }
              UnitSpawnPointOverride unitSpawnPointOverride = originalUnitSpawnPointOverride.DeepCopy();
              unitSpawnPointOverride.customUnitName = "";

              Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Using unitDefId '{unitSpawnPointOverride.unitDefId}' as a base");

              lanceOverride.unitSpawnPointOverrideList.Add(unitSpawnPointOverride);
            }
          } else {
            UnitSpawnPointOverride emptyUnitSpawnPointOverride = new UnitSpawnPointOverride();

            for (int i = numberOfUnitsInLance; i < factionLanceSize; i++) {
              Main.LogDebug($"[AddExtraLanceMembers] [{teamOverride.faction}] Non-autofill mode. Expanding lance size for position {i + 1} with a placeholder empty unit override.");
              lanceOverride.unitSpawnPointOverrideList.Add(emptyUnitSpawnPointOverride.DeepCopy());
            }
          }
        }
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