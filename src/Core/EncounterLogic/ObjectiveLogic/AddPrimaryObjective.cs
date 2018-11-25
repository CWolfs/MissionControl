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
      Main.Logger.Log($"[AddPrimaryObjective] Adding objective '{objectiveGuid}' as a primary objective");
      ContractOverride contractOverride = ((ContractOverridePayload)payload).ContractOverride;
      ObjectiveOverride objectiveOverride = new ObjectiveOverride();
      
      ObjectiveRef objectiveRef = new ObjectiveRef();
      objectiveRef.EncounterObjectGuid = objectiveGuid;
      objectiveOverride.objective = objectiveRef;

      objectiveOverride.isPrimary = true;

      contractOverride.objectiveList.Add(objectiveOverride);
    }
  }
}