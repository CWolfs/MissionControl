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

    public ChunkTrigger(MessageCenterMessageType onMessage, string chunkGuid) {
      this.onMessage = onMessage;
      this.chunkGuid = chunkGuid;
      ChunkMatchesChunkGuidConditional chunkConditional = ScriptableObject.CreateInstance<ChunkMatchesChunkGuidConditional>();
      chunkConditional.ChunkGuid = chunkGuid;
      this.conditional = chunkConditional;
    }

    public ChunkTrigger(MessageCenterMessageType onMessage, string chunkGuid, DesignConditional conditional) {
      this.onMessage = onMessage;
      this.chunkGuid = chunkGuid;
      this.conditional = conditional;
    }

    public override void Run(RunPayload payload) {
      Main.LogDebug("[ChunkTrigger] Running trigger");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse trigger = new SmartTriggerResponse();
      trigger.inputMessage = onMessage;
      trigger.designName = $"Initiate chunk on {onMessage}";
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