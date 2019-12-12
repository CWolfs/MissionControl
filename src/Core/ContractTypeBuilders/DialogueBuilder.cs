using UnityEngine;

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
    private bool showOnlyOnce;

    public DialogueBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject objective) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.objective = objective;

      this.parent = parent;
      this.name = objective["Name"].ToString();
      this.subType = objective["SubType"].ToString();
      this.guid = objective["Guid"].ToString();
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
    }
  }
}