using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;
using MissionControl.Trigger;
using MissionControl.Messages;

namespace MissionControl.Logic {
  public class AddDynamicWithdrawBatch {
    public LogicState state = new LogicState();
    
    public AddDynamicWithdrawBatch(EncounterRules encounterRules) {
      Main.Logger.Log($"[{this.GetType().Name}] Building Dynamic Withdraw");
      encounterRules.EncounterLogic.Add(new DoesChunkExist(state, "Chunk_Escape"));
      encounterRules.EncounterLogic.Add(new AddEscapeChunk(state, ChunkLogic.DYNAMIC_WITHDRAW_CHUNK_GUID, ChunkLogic.DYNAMIC_WITHDRAW_OBJECTIVE_GUID));
      encounterRules.EncounterLogic.Add(new ChunkTrigger((MessageCenterMessageType)MessageTypes.ON_CHUNK_ACTIVATED, ChunkLogic.DYNAMIC_WITHDRAW_CHUNK_GUID));
    }
  }
}