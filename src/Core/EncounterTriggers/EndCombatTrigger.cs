using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Result;
using MissionControl.Conditional;
using MissionControl.Logic;

namespace MissionControl.Trigger {
  public class EndCombatTrigger : EncounterTrigger {
    private MessageCenterMessageType onMessage;
    private string objectiveGuid;
    private DesignConditional conditional;

    public EndCombatTrigger(MessageCenterMessageType onMessage, string objectiveGuid) {
      this.onMessage = onMessage;
      this.objectiveGuid = objectiveGuid;
      
      ObjectiveStatusConditional objectiveStatusConditional = ScriptableObject.CreateInstance<ObjectiveStatusConditional>();
      objectiveStatusConditional.objective.EncounterObjectGuid = objectiveGuid;
      objectiveStatusConditional.objectiveStatus = ObjectiveStatusEvaluationType.Success;

      this.conditional = objectiveStatusConditional;
    }

    public EndCombatTrigger(MessageCenterMessageType onMessage, string objectiveGuid, DesignConditional conditional) {
      this.onMessage = onMessage;
      this.objectiveGuid = objectiveGuid;
      this.conditional = conditional;
    }

    public override void Run(RunPayload payload) {
      Main.LogDebug("[EndCombatTrigger] Running trigger");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse trigger = new SmartTriggerResponse();
      trigger.inputMessage = onMessage;
      trigger.designName = $"End combat on '{onMessage}'";
      trigger.conditionalbox = new EncounterConditionalBox(conditional);

      EndCombatResult endCombatResult = ScriptableObject.CreateInstance<EndCombatResult>();

      trigger.resultList.contentsBox.Add(new EncounterResultBox(endCombatResult));
      encounterData.responseGroup.triggerList.Add(trigger);
    }
  }
}