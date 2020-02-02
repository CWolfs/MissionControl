using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;
using BattleTech.Designed;
using BattleTech.Framework.Save;

using HBS.Collections;

using MissionControl.Result;
using MissionControl.Conditional;
using MissionControl.Logic;
using MissionControl.Messages;

namespace MissionControl.Trigger {
  public class WithdrawChunkTrigger : EncounterTrigger {
    private MessageCenterMessageType onMessage;
    private string chunkGuid;
    private DesignConditional conditional;

    public WithdrawChunkTrigger(MessageCenterMessageType onMessage, string chunkGuid) {
      this.onMessage = onMessage;
      this.chunkGuid = chunkGuid;
      ChunkMatchesChunkGuidConditional chunkConditional = ScriptableObject.CreateInstance<ChunkMatchesChunkGuidConditional>();
      chunkConditional.ChunkGuid = chunkGuid;
      this.conditional = chunkConditional;
    }

    public WithdrawChunkTrigger(MessageCenterMessageType onMessage, string chunkGuid, DesignConditional conditional) {
      this.onMessage = onMessage;
      this.chunkGuid = chunkGuid;
      this.conditional = conditional;
    }

    public override void Run(RunPayload payload) {
      Main.LogDebug("[ChunkTrigger] Setting up trigger");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse trigger = new SmartTriggerResponse();
      trigger.inputMessage = onMessage;
      trigger.designName = $"Initiate chunk on {(MessageTypes)onMessage}";
      trigger.conditionalbox = new EncounterConditionalBox(conditional);

      PositionRegionResult positionRegionResult = ScriptableObject.CreateInstance<PositionRegionResult>();
      positionRegionResult.RegionName = "Region_Withdraw";

      FailObjectivesResult failObjectivesResult = ScriptableObject.CreateInstance<FailObjectivesResult>();
      failObjectivesResult.ObjectiveNameWhiteList.Add("Objective_Withdraw");

      HACK_ActivateChunkResult activateChunkResult = ScriptableObject.CreateInstance<HACK_ActivateChunkResult>();
      EncounterChunkRef encounterChunkRef = new EncounterChunkRef();
      encounterChunkRef.EncounterObjectGuid = chunkGuid;
      activateChunkResult.encounterChunk = encounterChunkRef;

      trigger.resultList.contentsBox.Add(new EncounterResultBox(positionRegionResult));
      trigger.resultList.contentsBox.Add(new EncounterResultBox(failObjectivesResult));
      trigger.resultList.contentsBox.Add(new EncounterResultBox(activateChunkResult));
      encounterData.responseGroup.triggerList.Add(trigger);
    }
  }
}