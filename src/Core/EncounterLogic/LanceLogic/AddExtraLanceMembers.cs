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

      IncreaseLanceMembers(contractOverride, targetTeamOverride);
    }

    private void IncreaseLanceMembers(ContractOverride contractOverride, TeamOverride teamOverride) {
      List<LanceOverride> lanceOverrides = teamOverride.lanceOverrideList;
      int factionLanceSize = Main.Settings.ExtendedLances.GetFactionLanceSize(teamOverride.faction.ToString());
      Main.LogDebug($"[IncreaseLanceMembers] Faction '{teamOverride.faction}' lance size is '{factionLanceSize}");

      foreach (LanceOverride lanceOverride in lanceOverrides) {
        int numberOfUnitsInLance = lanceOverride.unitSpawnPointOverrideList.Count;
        bool isLanceTagged = lanceOverride.lanceDefId == "Tagged";
        bool isFirstLanceUnitTagged = lanceOverride.unitSpawnPointOverrideList[0].unitDefId == "Tagged";

        // If tagged, then a lance is selected from the 'data/lance' folder. 
        // If not, we want to add a new lance member if the vanilla lance size isn't large enough
        //  - If the lance members are 'tagged', then we'll copy the last one of those
        //  - If the lance members are 'manual', then we'll do nothing and send an error saying this should be fixed by the modder
        if (numberOfUnitsInLance < factionLanceSize) {
          Main.LogDebug($"[IncreaseLanceMembers] Override lance size is '{numberOfUnitsInLance}' but '{factionLanceSize}' is required. Adding more units to lance");
          if (!isLanceTagged && isFirstLanceUnitTagged) {
            lanceOverride.unitSpawnPointOverrideList.Add(lanceOverride.unitSpawnPointOverrideList[0].DeepCopy());
          } else if (!isLanceTagged && !isFirstLanceUnitTagged) {
            Main.Logger.LogError($"[AddExtraLanceMembers] The contract '{contractOverride.ID}' for the team '{teamOverride.teamName}' has a manual lance and manual unit setup but it does not specify the right number of lance members. When manually setting lances they should match the Mission Control ExtendedLance lance member count. For this lance you should have exactly '{factionLanceSize}' but only '{numberOfUnitsInLance}' are set.");
          }
        }
      }
    }
  }
}