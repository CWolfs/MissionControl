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

    public DialogTrigger(MessageCenterMessageType onMessage, string dialogueGuid) {
      this.onMessage = onMessage;
      this.dialogueGuid = dialogueGuid;
    }

    public override void Run(RunPayload payload) {
      Main.LogDebug("[DialogTrigger] Running trigger");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse triggerDialogue = new SmartTriggerResponse();
      triggerDialogue.inputMessage = onMessage;
      triggerDialogue.designName = $"Initiate dialogue on {triggerDialogue}";
      triggerDialogue.conditionalbox = new EncounterConditionalBox(ScriptableObject.CreateInstance<AlwaysTrueConditional>());

      DialogResult dialogueResult = ScriptableObject.CreateInstance<DialogResult>();
      dialogueResult.dialogueRef.EncounterObjectGuid = dialogueGuid;
      
      triggerDialogue.resultList.contentsBox.Add(new EncounterResultBox(dialogueResult));
      encounterData.responseGroup.triggerList.Add(triggerDialogue);
    }
  }
}