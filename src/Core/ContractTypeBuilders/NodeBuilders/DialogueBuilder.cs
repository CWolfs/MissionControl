using UnityEngine;

using System;
using System.Collections.Generic;

using MissionControl.Trigger;
using MissionControl.EncounterFactories;
using MissionControl.Messages;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class DialogueBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject dialogue;

    private GameObject parent;
    private string name;
    private string subType;
    private string guid;

    public DialogueBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject dialogue) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.dialogue = dialogue;

      this.parent = parent;
      this.name = dialogue["Name"].ToString();
      this.subType = dialogue["SubType"].ToString();
      this.guid = dialogue["Guid"].ToString();
    }

    public override void Build() {
      switch (subType) {
        case "Simple": BuildSimpleDialogue(parent, dialogue); break;
        case "Sequence": BuildSequenceDialogue(parent, dialogue); break;
        default: Main.LogDebug($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildSimpleDialogue(GameObject parent, JObject dialogue) {
      string trigger = dialogue.ContainsKey("Trigger") ? dialogue["Trigger"].ToString() : null;
      bool showOnlyOnce = dialogue.ContainsKey("ShowOnlyOnce") ? (bool)dialogue["ShowOnlyOnce"] : false;

      DialogueFactory.CreateDialogLogic(parent, this.name, this.guid, showOnlyOnce);

      MessageCenterMessageType messageType;
      if (trigger != null) {
        if (!Enum.TryParse(trigger, out messageType)) {
          MessageTypes customMessageType;
          if (!Enum.TryParse(trigger, out customMessageType)) {
            Main.Logger.LogError("[DialogueBuilder] Invalid 'Trigger' provided.");
          } else {
            messageType = (MessageCenterMessageType)customMessageType;
          }
        }

        DialogTrigger dialogueTrigger = new DialogTrigger(messageType, this.guid);
        dialogueTrigger.Run();
      }
    }

    private void BuildSequenceDialogue(GameObject parent, JObject dialogue) {
      bool isInterrupt = dialogue.ContainsKey("IsInterrupt") ? (bool)dialogue["IsInterrupt"] : true;
      List<string> dialogueGUIDs = dialogue.ContainsKey("DialogueGUIDs") ? dialogue["DialogueGUIDs"].ToObject<List<string>>() : null;

      DialogueFactory.CreateDialogueSequenceLogic(parent, this.name, this.guid, isInterrupt, dialogueGUIDs);
    }
  }
}