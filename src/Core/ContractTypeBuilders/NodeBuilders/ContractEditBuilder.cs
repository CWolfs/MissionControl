using UnityEngine;

using System;

using MissionControl.EncounterFactories;
using MissionControl.LogicComponents.ContractEdits;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class ContractEditBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject objective;

    private GameObject parent;
    private string name;
    private string subType;

    public ContractEditBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject objective) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.objective = objective;

      this.parent = parent;
      this.name = objective["Name"].ToString();
      this.subType = objective["SubType"].ToString();
    }

    public override void Build() {
      switch (subType) {
        case "SwapTeamFactions": BuildSwapFactions(); break;
        default: Main.LogDebug($"[RegionBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildSwapFactions() {
      string teamGuid1 = objective["TeamGuid1"].ToString();
      string teamGuid2 = objective["TeamGuid2"].ToString();

      SwapTeamFactionGameLogic swapFactionGameLogic = ContractEditFactory.CreateSwapFaction(this.parent, this.name, Guid.NewGuid().ToString(), teamGuid1, teamGuid2);
    }
  }
}