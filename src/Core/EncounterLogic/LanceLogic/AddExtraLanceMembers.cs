using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class AddExtraLanceMembers : LanceLogic {

    public AddExtraLanceMembers() { }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddExtraLanceMembers] Adding extra lance units to lance");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;

      TeamOverride targetTeamOverride = contractOverride.targetTeam;
      TeamOverride employerTeamOverride = contractOverride.employerTeam;

      IncreaseLanceMembers(contractOverride, targetTeamOverride);
      IncreaseLanceMembers(contractOverride, employerTeamOverride);
    }

    private void IncreaseLanceMembers(ContractOverride contractOverride, TeamOverride teamOverride) {
      List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;
      int factionLanceSize = Main.Settings.ExtendedLances.GetFactionLanceSize(teamOverride.faction.ToString());
      Main.LogDebug($"[IncreaseLanceMembers] Faction '{teamOverride.faction}' lance size is '{factionLanceSize}");

      foreach (LanceOverride lanceOverride in lanceOverrides) {
        int numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;
        bool isLanceTagged = lanceOverride.lanceDefId == "Tagged" || lanceOverride.lanceDefId == "UseLance";
        bool AreAnyLanceUnitsTagged = AreAnyLanceMembersTagged(lanceOverride);

        // If tagged, then a lance is selected from the 'data/lance' folder. If we need to increase size we do it later for this usecase.
        // If not, we want to add a new lance member if the vanilla lance size isn't large enough
        //  - If the lance members are 'tagged', then we'll copy any of the tagged units as a base
        //  - If the lance members are 'manual', then do nothing and let the later code handle this usecase
        if (numberOfUnitsInLance < factionLanceSize) {
          Main.LogDebug($"[IncreaseLanceMembers] Override lance size is '{numberOfUnitsInLance}' but '{factionLanceSize}' is required. Adding more units to lance");
          if (!isLanceTagged && AreAnyLanceUnitsTagged) {
            lanceOverride.unitSpawnPointOverrideList.Add(lanceOverride.GetAnyTaggedLanceMember().DeepCopy());
          } else if (!isLanceTagged && !AreAnyLanceUnitsTagged) {
            Main.Logger.LogWarning($"[AddExtraLanceMembers] The contract '{contractOverride.ID}' for the team '{teamOverride.teamName}' has a manual lance and manual unit setup but it does not specify the right number of lance members. When manually setting lances they should match the Mission Control ExtendedLance lance member count. For this lance you should probably have exactly '{factionLanceSize}' but only '{numberOfUnitsInLance}' are set.");
          }
        }
      }
    }

    private bool AreAnyLanceMembersTagged(LanceOverride lanceOverride) {
      UnitSpawnPointOverride unitSpawnOverrides = lanceOverride.GetAnyTaggedLanceMember();
      return (unitSpawnOverrides != null);
    }
  }
}