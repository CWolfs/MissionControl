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
  public class DialogTrigger : EncounterTrigger {
    private MessageCenterMessageType onMessage;
    private string dialogueGuid;
    private bool isInterrupt;
    private DesignConditional conditional;

    public DialogTrigger(MessageCenterMessageType onMessage, string dialogueGuid) {
      this.onMessage = onMessage;
      this.dialogueGuid = dialogueGuid;
      this.isInterrupt = true;
      this.conditional = ScriptableObject.CreateInstance<AlwaysTrueConditional>();
    }

    public DialogTrigger(MessageCenterMessageType onMessage, string dialogueGuid, bool isInterrupt) {
      this.onMessage = onMessage;
      this.dialogueGuid = dialogueGuid;
      this.isInterrupt = isInterrupt;
      this.conditional = ScriptableObject.CreateInstance<AlwaysTrueConditional>();
    }

    public DialogTrigger(MessageCenterMessageType onMessage, string dialogueGuid, bool isInterrupt, DesignConditional conditional) {
      this.onMessage = onMessage;
      this.dialogueGuid = dialogueGuid;
      this.isInterrupt = isInterrupt;
      this.conditional = conditional;
    }

    public override void Run(RunPayload payload) {
      Main.LogDebug("[DialogTrigger] Running trigger");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse triggerDialogue = new SmartTriggerResponse();
      triggerDialogue.inputMessage = onMessage;
      triggerDialogue.designName = $"Initiate dialogue on {triggerDialogue}";
      triggerDialogue.conditionalbox = new EncounterConditionalBox(conditional);

      DialogResult dialogueResult = ScriptableObject.CreateInstance<DialogResult>();
      dialogueResult.dialogueRef.EncounterObjectGuid = dialogueGuid;
      dialogueResult.isInterrupt = this.isInterrupt;
      
      triggerDialogue.resultList.contentsBox.Add(new EncounterResultBox(dialogueResult));
      encounterData.responseGroup.triggerList.Add(triggerDialogue);
    }
  }
}