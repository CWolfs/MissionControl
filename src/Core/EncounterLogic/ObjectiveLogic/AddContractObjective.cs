using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Logic {
  public class AddContractObjective : ObjectiveLogic {
    private string objectiveGuid;
    private bool isPrimary;
    private string title;
    private string description;

    public AddContractObjective(string objectiveGuid, bool isPrimary) {
      this.objectiveGuid = objectiveGuid;
      this.isPrimary = isPrimary;
      this.title = "";
      this.description = "";
    }

    public AddContractObjective(string objectiveGuid, bool isPrimary, string title, string description) {
      this.objectiveGuid = objectiveGuid;
      this.isPrimary = isPrimary;
      this.title = title;
      this.description = description;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddContractObjective] Adding contract objective '{objectiveGuid}' as a primary objective");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;
      ContractObjectiveOverride contractObjectiveOverride = new ContractObjectiveOverride();
      
      // ObjectiveRef objectiveRef = new ObjectiveRef();
      // objectiveRef.EncounterObjectGuid = objectiveGuid;
      // contractObjectiveOverride.objective = objectiveRef;
      contractObjectiveOverride.SetContractContext(contractOverride.contract);
      contractObjectiveOverride.isPrimary = isPrimary;
      contractObjectiveOverride.title = title;
      contractObjectiveOverride.description = description;

      contractOverride.contractObjectiveList.Add(contractObjectiveOverride);
    }
  }
}