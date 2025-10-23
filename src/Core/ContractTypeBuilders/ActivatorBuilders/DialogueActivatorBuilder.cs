using UnityEngine;

using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class DialogueActivatorBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject activator;

    private GameObject parent;
    private string dialogueGuid;
    private bool isInterrupt = true;

    public DialogueActivatorBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject activator) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.activator = activator;

      this.parent = parent;
      this.dialogueGuid = activator["EncounterGuid"].ToString();
      this.isInterrupt = activator.ContainsKey("IsInterrupt") ? activator["IsInterrupt"].ToObject<bool>() : true;
    }

    public override void Build() {
      DialogueFactory.CreateDialogueActivator(this.parent, this.dialogueGuid, this.isInterrupt);
    }
  }
}