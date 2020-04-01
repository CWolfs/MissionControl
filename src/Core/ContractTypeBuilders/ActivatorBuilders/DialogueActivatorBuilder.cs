using UnityEngine;

using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class DialogueActivatorBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject activator;

    private GameObject parent;
    private string dialogueGuid;

    public DialogueActivatorBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject activator) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.activator = activator;

      this.parent = parent;
      this.dialogueGuid = activator["EncounterGuid"].ToString();
    }

    public override void Build() {
      DialogueFactory.CreateDialogueActivator(this.parent, this.dialogueGuid);
    }
  }
}