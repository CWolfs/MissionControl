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

      TeamOverride teamOverride = contractOverride.GetTeamOverrideLanceBelongsTo(lanceOverride.GUID);
      Main.Logger.Log($"[AddExtraLanceMembersIndividualSecondPass] Team Override for lance '{lanceOverride.name} - {lanceOverride.GUID}' is: {teamOverride.teamName}");

      lancesToSkip = (List<string>)state.GetObject("LancesToSkip");

      // Check first pass LanceOverride skips and check LanceDef skips in this second pass, if one exists, and check tags for skipping or override
      if (!isManualLance) {
        LanceDef loadedLanceDef = (LanceDef)AccessTools.Field(typeof(LanceOverride), "loadedLanceDef").GetValue(lanceOverride);
        bool skip = CheckForLanceOverrideSkips(lanceOverride, teamOverride, lanceOverride.GUID);
        if (!skip) skip = CheckForLanceDefSkips(loadedLanceDef, teamOverride, lanceOverride.GUID);

        if (skip) return;
      }

      Main.Logger.Log($"[AddExtraLanceMembersIndividualSecondPass] No Skips Detected. Processing second pass.");

      // Check for LanceDef tags to force LanceDef to override the EL lance unit count
      if (!isManualLance) {
        // No manual changes here as first pass sets them
      }
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