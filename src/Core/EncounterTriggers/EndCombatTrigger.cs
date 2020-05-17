using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Result;
using MissionControl.Conditional;
using MissionControl.Logic;

namespace MissionControl.Trigger {
  public class EndCombatTrigger : EncounterTrigger {
    public enum EndCombatType { SUCCESS, FAILURE, RETREAT };

    private MessageCenterMessageType onMessage;
    private string objectiveGuid;
    private DesignConditional conditional;
    private EndCombatType type;

    public EndCombatTrigger(MessageCenterMessageType onMessage, string objectiveGuid, EndCombatType type) {
      this.onMessage = onMessage;
      this.objectiveGuid = objectiveGuid;
      this.type = type;

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
      Main.LogDebug("[EndCombatTrigger] Setting up trigger");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse trigger = new SmartTriggerResponse();
      trigger.inputMessage = onMessage;
      trigger.designName = $"End combat on '{onMessage}'";
      trigger.conditionalbox = new EncounterConditionalBox(conditional);

      DesignResult result = null;
      if (type == EndCombatType.RETREAT) {
        result = ScriptableObject.CreateInstance<EndCombatRetreatResult>();
      } else {
        // Fallback to the only end combat we currently have - Retreat
        result = ScriptableObject.CreateInstance<EndCombatRetreatResult>();
      }

      trigger.resultList.contentsBox.Add(new EncounterResultBox(result));
      encounterData.responseGroup.triggerList.Add(trigger);
    }
  }
}