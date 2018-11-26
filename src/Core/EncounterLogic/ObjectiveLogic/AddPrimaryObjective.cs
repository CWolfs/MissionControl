using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Logic {
  public class AddPrimaryObjective : ObjectiveLogic {
    private string objectiveGuid;

    public AddPrimaryObjective(string objectiveGuid) {
      this.objectiveGuid = objectiveGuid;
    }

    public override void Run(RunPayload payload) {
      // NOT IMPLEMENTED
    }

    /*
    // This adds the contract objective early enough so it displays on the loading screen
    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddPrimaryObjective] Adding objective '{objectiveGuid}' as a primary objective");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;
      ContractObjectiveOverride contractObjectiveOverride = new ContractObjectiveOverride();
      
      ObjectiveRef objectiveRef = new ObjectiveRef();
      objectiveRef.EncounterObjectGuid = objectiveGuid;
      // contractObjectiveOverride.objective = objectiveRef;
      contractObjectiveOverride.isPrimary = true;
      contractObjectiveOverride.title = "Test";
      contractObjectiveOverride.description = "Test Description";

      contractOverride.contractObjectiveList.Add(contractObjectiveOverride);
    }
    */
  }
}