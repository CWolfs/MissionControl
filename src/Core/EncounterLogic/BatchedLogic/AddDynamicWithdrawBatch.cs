using UnityEngine;
using System;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;
using MissionControl.Trigger;
using MissionControl.Messages;

namespace MissionControl.Logic {
  public class AddDynamicWithdrawBatch {
    public LogicState state = new LogicState();
    
    public AddDynamicWithdrawBatch(EncounterRules encounterRules) {
      Main.Logger.Log($"[{this.GetType().Name}] Building Dynamic Withdraw");
      encounterRules.EncounterLogic.Add(new DoesChunkExist(state, "Chunk_Escape"));
      encounterRules.EncounterLogic.Add(new AddEscapeChunk(state, ChunkLogic.DYNAMIC_WITHDRAW_CHUNK_GUID, ChunkLogic.DYNAMIC_WITHDRAW_OBJECTIVE_GUID, ChunkLogic.DYNAMIC_WITHDRAW_REGION_GUID));
      encounterRules.EncounterLogic.Add(new ChunkTrigger((MessageCenterMessageType)MessageTypes.ON_CHUNK_ACTIVATED, ChunkLogic.DYNAMIC_WITHDRAW_CHUNK_GUID));
      encounterRules.EncounterLogic.Add(new EndCombatTrigger(MessageCenterMessageType.OnObjectiveSucceeded, ChunkLogic.DYNAMIC_WITHDRAW_OBJECTIVE_GUID));

      encounterRules.EncounterLogic.Add(new AddDialogueChunk(
        ChunkLogic.DIALOGUE_DYNAMIC_WITHDRAW_ESCAPE_GUID,
        "DynamicWithdrawEscape",
        "Start Conversation For Dynamic Withdraw Escape",
        ChunkLogic.DYNAMIC_WITHDRAW_REGION_GUID,
        true,
        "I'm coming in hot. Get to the EZ as soon as you can, Commander",
        UnityGameInstance.BattleTechGame.DataManager.CastDefs.Get("castDef_SumireDefault")
      ));
      encounterRules.EncounterLogic.Add(new DialogTrigger((MessageCenterMessageType)MessageTypes.ON_CHUNK_ACTIVATED, ChunkLogic.DIALOGUE_DYNAMIC_WITHDRAW_ESCAPE_GUID));
    }
  }
}