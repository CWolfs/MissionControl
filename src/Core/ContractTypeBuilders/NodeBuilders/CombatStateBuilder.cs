using UnityEngine;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class CombatStateBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject state;

    private GameObject parent;
    private string name;
    private string subType;

    public CombatStateBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject state) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.state = state;

      this.parent = parent;
      this.name = state["Name"].ToString();
      this.subType = state["SubType"].ToString();
    }

    public override void Build() {
      switch (subType) {
        case "DisablePilotDeath": BuildDisablePilotDeath(); break;
        default: Main.LogDebug($"[CombatStateBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildDisablePilotDeath() {
      //SwapTeamFactionGameLogic swapFactionGameLogic = ContractEditFactory.CreateSwapFaction(this.parent, this.name, Guid.NewGuid().ToString(), teamGuid1, teamGuid2);
    }
  }
}