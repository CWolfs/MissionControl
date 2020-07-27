using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Logic {
  /*
  * A Full Contract Objective injects earlier in the loading flow
  * This allows for a contract objective to be added to the encounter and also displays it in the loading screen
  */
  public class AddContractObjective : ObjectiveLogic {
    private string contractObjectiveGuid;
    private bool isPrimary;
    private string title;
    private string description;
    public List<string> ObjectiveGuids { get; set; } = new List<string>();

    public AddContractObjective(string contractObjectiveGuid, bool isPrimary) {
      this.contractObjectiveGuid = contractObjectiveGuid;
      this.isPrimary = isPrimary;
      this.title = "";
      this.description = "";
    }

    public AddContractObjective(string contractObjectiveGuid, bool isPrimary, string title, string description) {
      this.contractObjectiveGuid = contractObjectiveGuid;
      this.isPrimary = isPrimary;
      this.title = title;
      this.description = description;
    }

    public override void Run(RunPayload payload) {
      string objectiveTypeLabel = isPrimary ? "primary" : "secondary";
      Main.Logger.Log($"[AddContractObjective] Adding contract objective '{contractObjectiveGuid}' as a {objectiveTypeLabel} objective");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;
      ContractObjectiveOverride contractObjectiveOverride = new ContractObjectiveOverride();

      ContractObjectiveRef contractObjectiveRef = new ContractObjectiveRef();
      contractObjectiveRef.EncounterObjectGuid = contractObjectiveGuid;
      contractObjectiveOverride.contractObjective = contractObjectiveRef;

      contractObjectiveOverride.isPrimary = isPrimary;
      contractObjectiveOverride.title = title;
      contractObjectiveOverride.description = "MC" + description;  // Important and used for objective cleanup
      contractObjectiveOverride.objectiveGuids = ObjectiveGuids;
      contractObjectiveOverride.forPlayer = TeamController.Player1;

      contractOverride.contractObjectiveList.Add(contractObjectiveOverride);
      contractObjectiveOverride.SetContractContext(contractOverride.contract);
    }
  }
}