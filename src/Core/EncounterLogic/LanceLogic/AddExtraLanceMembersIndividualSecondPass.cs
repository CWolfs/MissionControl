using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using Harmony;

namespace MissionControl.Logic {
  public class AddExtraLanceMembersIndividualSecondPass : RequestLanceCompleteLogic {
    private LogicState state;
    private List<string> lancesToSkip = new List<string>();

    public AddExtraLanceMembersIndividualSecondPass(LogicState state) {
      this.state = state;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddExtraLanceMembersIndividualSecondPass] Running second pass after LanceDef has resolved, if required");
      ContractOverride contractOverride = ((ContractAndLanceOverridePayload)payload).ContractOverride;
      LanceOverride lanceOverride = ((ContractAndLanceOverridePayload)payload).LanceOverride;
      bool isManualLance = lanceOverride.lanceDefId == "Manual";
      int currentLanceSize = lanceOverride.unitSpawnPointOverrideList.Count;

      TeamOverride teamOverride = contractOverride.GetTeamOverrideLanceBelongsTo(lanceOverride.GUID);
      Main.Logger.Log($"[AddExtraLanceMembersIndividualSecondPass] Team Override for lance '{lanceOverride.name} - {lanceOverride.GUID}' is: {teamOverride.teamName}");

      lancesToSkip = (List<string>)state.GetObject("LancesToSkip");

      // Check first pass LanceOverride skips and check LanceDef skips in this second pass, if one exists, and check tags for skipping or override
      if (!isManualLance) {
        LanceDef loadedLanceDef = (LanceDef)AccessTools.Field(typeof(LanceOverride), "loadedLanceDef").GetValue(lanceOverride);
        Main.Logger.Log($"[AddExtraLanceMembersIndividualSecondPass] Loaded LanceDef is '{loadedLanceDef.Description.Id}'");

        bool skip = CheckForLanceOverrideSkips(lanceOverride, teamOverride, lanceOverride.GUID);
        if (!skip) skip = CheckForLanceDefSkips(loadedLanceDef, teamOverride, lanceOverride.GUID);

        if (skip) return;


        Main.Logger.Log($"[AddExtraLanceMembersIndividualSecondPass] No Skips Detected. Processing second pass.");

        // Check for LanceDef tags to force LanceDef to override the EL lance unit count
        if (IsLanceDefForced(loadedLanceDef)) {
          this.state.Set($"LANCE_DEF_FORCED_{lanceOverride.GUID}", true);

          int newLanceSize = loadedLanceDef.LanceUnits.Length;
          Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] Force overriding lance def '{lanceOverride.name}' from faction size of '{currentLanceSize}' to '{newLanceSize}'");

          if (newLanceSize < currentLanceSize) {
            // Remove UnitOverrides. Last to First. This is because the override in the LanceDef has fewer units that the EL faction size, or forced Contract Override size
            for (int i = currentLanceSize - 1; i >= newLanceSize; i--) {
              Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] Removing UnitOverride '{i}' from LanceOverride");
              lanceOverride.unitSpawnPointOverrideList.RemoveAt(i);
            }
          } else if (newLanceSize > currentLanceSize) {
            // Add UnitOverrides. This is because the override in the LanceDef has more units that the EL faction size, or forced Contract Override size
            // This allows the LanceOverride to allocated the correct LanceDef units to the right UnitOverride slots
            UnitSpawnPointOverride emptyUnitSpawnPointOverride = new UnitSpawnPointOverride();

            for (int i = currentLanceSize; i < newLanceSize; i++) {
              Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] Adding UnitOverride '{i}' to LanceOverride");
              lanceOverride.unitSpawnPointOverrideList.Add(emptyUnitSpawnPointOverride.DeepCopy());
            }
          }
        }
      }
    }

    // Checks if the LanceDef lance unit count should be used to override the Faction lance unit count
    // It does this by checking a set tag. If it's present then it will be forced.
    private bool IsLanceDefForced(LanceDef lanceDef) {
      if (lanceDef.LanceTags.GetTagSetSourceFile().Contains(Main.Settings.ExtendedLances.ForceLanceDefSizeWithTag)) {
        Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] TagSetSourceFile '{lanceDef.LanceTags.GetTagSetSourceFile()}' contains tag '{Main.Settings.ExtendedLances.ForceLanceDefSizeWithTag}'");
        return true;
      } else {
        Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] TagSetSourceFile '{lanceDef.LanceTags.GetTagSetSourceFile()}' DOES NOT contain tag '{Main.Settings.ExtendedLances.ForceLanceDefSizeWithTag}'");
      }
      return false;
    }

    private bool CheckForLanceOverrideSkips(LanceOverride lanceOverride, TeamOverride teamOverride, string lanceGUID) {
      Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] [{teamOverride.faction}] Checking first pass skips in second pass...");

      if (lancesToSkip.Contains(lanceOverride.GUID)) {
        Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] [Faction:{teamOverride.faction}] Detected a lance to skip. Skipping.");
        return true;
      }

      if (lanceOverride.IsATurretLance()) {
        Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] [Faction:{teamOverride.faction}] Detected a turret lance Ignoring for Extended Lances.");
        return true;
      }

      return false;
    }

    private bool CheckForLanceDefSkips(LanceDef loadedLanceDef, TeamOverride teamOverride, string lanceGUID) {
      Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] [{teamOverride.faction}] Checking loaded LanceDef '{loadedLanceDef.Description.Id}' for Lance Override '{lanceGUID}'...");

      if (Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAny().Count > 0 && loadedLanceDef.LanceTags.ContainsAny(Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAny())) {
        Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] [{teamOverride.faction}] LanceDef contains a tag set in 'SkipWhenTaggedWithAny'. Skipping '{loadedLanceDef.Description.Id}'");
        lancesToSkip.Add(lanceGUID);
        return true;
      }

      if (Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAll().Count > 0 && loadedLanceDef.LanceTags.ContainsAll(Main.Settings.ExtendedLances.GetSkipWhenTaggedWithAll())) {
        Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] [{teamOverride.faction}] Lance contains a tag set in 'SkipWhenTaggedWithAll'. Skipping '{loadedLanceDef.Description.Id}'");
        lancesToSkip.Add(lanceGUID);
        return true;
      }

      if (Main.Settings.ExtendedLances.GetSkipWhenExcludeTagsContain().Count > 0 && loadedLanceDef.LanceTags.ContainsAny(Main.Settings.ExtendedLances.GetSkipWhenExcludeTagsContain())) {
        Main.LogDebug($"[AddExtraLanceMembersIndividualSecondPass] [{teamOverride.faction}] Lance contains an exclude tag set in 'GetSkipWhenExcludeTagsContain'. Skipping '{loadedLanceDef.Description.Id}'");
        lancesToSkip.Add(lanceGUID);
        return true;
      }

      return false;
    }
  }
}