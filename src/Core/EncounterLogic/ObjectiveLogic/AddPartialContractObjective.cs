using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Logic {
  /*
  * A Partial Contract Objective injects later in the loading flow
  * This allows for a contract objective to be added to the encounter but hides it from the loading screen
  */
  public class AddPartialContractObjective : ChunkLogic {
    private string contractObjectiveGuid;
    private bool isPrimary;
    private string title;
    private string description;
    public List<string> ObjectiveGuids { get; set; } = new List<string>();

    public AddPartialContractObjective(string contractObjectiveGuid, bool isPrimary) {
      this.contractObjectiveGuid = contractObjectiveGuid;
      this.isPrimary = isPrimary;
      this.title = "";
      this.description = "";
    }

    public AddPartialContractObjective(string contractObjectiveGuid, bool isPrimary, string title, string description) {
      this.contractObjectiveGuid = contractObjectiveGuid;
      this.isPrimary = isPrimary;
      this.title = title;
      this.description = description;
    }

    public override void Run(RunPayload payload) {
      string objectiveTypeLabel = isPrimary ? "primary" : "secondary";
      Main.Logger.Log($"[AddPartialContractObjective] Adding contract objective '{contractObjectiveGuid}' as a {objectiveTypeLabel} objective");
      ContractOverride contractOverride = MissionControl.Instance.CurrentContract.Override;
      ContractObjectiveOverride contractObjectiveOverride = new ContractObjectiveOverride();

      ContractObjectiveRef contractObjectiveRef = new ContractObjectiveRef();
      contractObjectiveRef.EncounterObjectGuid = contractObjectiveGuid;
      contractObjectiveOverride.contractObjective = contractObjectiveRef;

      contractObjectiveOverride.isPrimary = isPrimary;
      contractObjectiveOverride.title = title;
      contractObjectiveOverride.description = description;
      contractObjectiveOverride.objectiveGuids = ObjectiveGuids;
      contractObjectiveOverride.forPlayer = TeamController.Player1;

      contractOverride.contractObjectiveList.Add(contractObjectiveOverride);
      contractObjectiveOverride.SetContractContext(contractOverride.contract);
    }
  }
}