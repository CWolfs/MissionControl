using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;
using BattleTech.Designed;

using HBS.Collections;

using MissionControl.Result;
using MissionControl.Conditional;
using MissionControl.Logic;

namespace MissionControl.Trigger {
  public class ChunkTrigger : EncounterTrigger {
    private MessageCenterMessageType onMessage;
    private string chunkGuid;
    private DesignConditional conditional;

    public ChunkTrigger(MessageCenterMessageType onMessage, string objectiveGuid) {
      this.onMessage = onMessage;
      this.chunkGuid = objectiveGuid;
      this.conditional = ScriptableObject.CreateInstance<AlwaysTrueConditional>();
    }

    public ChunkTrigger(MessageCenterMessageType onMessage, string objectiveGuid, DesignConditional conditional) {
      this.onMessage = onMessage;
      this.chunkGuid = objectiveGuid;
      this.conditional = conditional;
    }

    public override void Run(RunPayload payload) {
      Main.LogDebug("[ChunkTrigger] Running trigger");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse trigger = new SmartTriggerResponse();
      trigger.inputMessage = onMessage;
      trigger.designName = $"Initiate chunk on {trigger}";
      trigger.conditionalbox = new EncounterConditionalBox(conditional);

      HACK_ActivateChunkResult activateChunkResult = ScriptableObject.CreateInstance<HACK_ActivateChunkResult>();
      EncounterChunkRef encounterChunkRef = new EncounterChunkRef();
      encounterChunkRef.EncounterObjectGuid = chunkGuid;
      activateChunkResult.encounterChunk = encounterChunkRef;

      trigger.resultList.contentsBox.Add(new EncounterResultBox(activateChunkResult));
      encounterData.responseGroup.triggerList.Add(trigger);
    }
  }
}