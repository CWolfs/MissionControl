using UnityEngine;

using System;

using MissionControl.Trigger;
using MissionControl.Rules;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class DialogueBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject objective;

    private GameObject parent;
    private string name;
    private string subType;
    private string guid;
    private string trigger;
    private bool showOnlyOnce;

    public DialogueBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject objective) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.objective = objective;

      this.parent = parent;
      this.name = objective["Name"].ToString();
      this.subType = objective["SubType"].ToString();
      this.guid = objective["Guid"].ToString();
      this.trigger = objective["Trigger"].ToString();
      this.showOnlyOnce = objective.ContainsKey("ShowOnlyOnce") ? (bool)objective["ShowOnlyOnce"] : false;
    }

    public override void Build() {
      switch (subType) {
        case "Simple": BuildSimpleDialogue(); break;
        default: Main.LogDebug($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildSimpleDialogue() {
      DialogueFactory.CreateDialogLogic(this.parent, this.name, this.guid, this.showOnlyOnce);
      MessageCenterMessageType triggerMessageType = (MessageCenterMessageType)Enum.Parse(typeof(MessageCenterMessageType), this.trigger);
      DialogTrigger dialogueTrigger = new DialogTrigger(triggerMessageType, this.guid);
      dialogueTrigger.Run();
    }
  }
}