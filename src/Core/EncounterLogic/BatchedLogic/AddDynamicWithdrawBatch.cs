using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;
using MissionControl.Trigger;

namespace MissionControl.Logic {
  public class AddDynamicWithdrawBatch {
    public AddDynamicWithdrawBatch(EncounterRules encounterRules) {
      Main.Logger.Log($"[{this.GetType().Name}] Building Dynamic Withdraw");
      encounterRules.EncounterLogic.Add(new AddEscapeChunk());
    }
  }
}