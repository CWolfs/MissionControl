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
  public class ShowObjectiveTrigger : EncounterTrigger {
    private MessageCenterMessageType onMessage;
    private string lanceTargetGuid;
    private string objectiveGuid;
    private bool isContractObjective;
    private DesignConditional conditional;

    public ShowObjectiveTrigger(MessageCenterMessageType onMessage, string lanceTargetGuid, string objectiveGuid, bool isContractObjective) {
      this.onMessage = onMessage;
      this.lanceTargetGuid = lanceTargetGuid;
      this.objectiveGuid = objectiveGuid;
      this.isContractObjective = isContractObjective;
      LanceDetectedConditional lanceDetectedConditional = ScriptableObject.CreateInstance<LanceDetectedConditional>();
      lanceDetectedConditional.TargetLanceGuid = lanceTargetGuid;
      this.conditional = lanceDetectedConditional;
    }

    public ShowObjectiveTrigger(MessageCenterMessageType onMessage, string lanceTargetGuid, string objectiveGuid, bool isContractObjective, DesignConditional conditional) {
      this.onMessage = onMessage;
      this.lanceTargetGuid = lanceTargetGuid;
      this.objectiveGuid = objectiveGuid;
      this.isContractObjective = isContractObjective;
      this.conditional = conditional;
    }

    public override void Run(RunPayload payload) {
      Main.LogDebug("[ShowObjectiveTrigger] Setting up trigger");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse trigger = new SmartTriggerResponse();
      trigger.inputMessage = onMessage;
      trigger.designName = $"Show objective on {onMessage}";
      trigger.conditionalbox = new EncounterConditionalBox(conditional);

      ShowObjectiveResult showObjectiveResult = ScriptableObject.CreateInstance<ShowObjectiveResult>();
      showObjectiveResult.ObjectiveGuid = this.objectiveGuid;
      showObjectiveResult.IsContractObjective = this.isContractObjective;

      trigger.resultList.contentsBox.Add(new EncounterResultBox(showObjectiveResult));
      encounterData.responseGroup.triggerList.Add(trigger);
    }
  }
}