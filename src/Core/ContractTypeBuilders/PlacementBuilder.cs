using UnityEngine;

using System;

using MissionControl.EncounterFactories;
using MissionControl.LogicComponents.Placers;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class PlacementBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject objective;

    private GameObject parent;
    private string name;
    private string subType;
    private string targetGuid1;
    private string targetGuid2;

    public PlacementBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject objective) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.objective = objective;

      this.parent = parent;
      this.name = objective["Name"].ToString();
      this.subType = objective["SubType"].ToString();
      this.targetGuid1 = objective["TargetGuid1"].ToString();
      this.targetGuid2 = objective["TargetGuid2"].ToString();
    }

    public override void Build() {
      switch (subType) {
        case "EncounterStructure": BuildSwapPlacement(); break;
        default: Main.LogDebug($"[RegionBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildSwapPlacement() {
      SwapPlacementGameLogic swapSpawnLogic = PlacementFactory.CreateSwapSpawn(this.parent, this.name, Guid.NewGuid().ToString(), this.targetGuid1, this.targetGuid2);
    }
  }
}