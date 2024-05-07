using UnityEngine;

using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

using MissionControl.Data;
using MissionControl.EncounterFactories;
using MissionControl.EncounterNodes.Generator;

namespace MissionControl.ContractTypeBuilders {
  public class GeneratorBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject build;

    private GameObject parent;
    private string name;
    private string subType;
    private string guid;

    public GeneratorBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject build) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.build = build;

      this.parent = parent;
      this.name = build["Name"].ToString();
      this.subType = build["SubType"].ToString();
      this.guid = build["Guid"].ToString();
    }

    public override void Build() {
      switch (subType) {
        case "PowerGenerator": BuildPowerGenerator(); break;
        default: Main.LogDebug($"[GeneratorBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildPowerGenerator() {
      List<string> generatorTags = ((JArray)build["GeneratorTags"]).ToObject<List<string>>();
      List<string> powerConsumersTags = ((JArray)build["PowerConsumersTags"]).ToObject<List<string>>();
      string onGeneratorDestructionTypeRaw = build.ContainsKey("OnGeneratorDestructionType") ? build["OnGeneratorDestructionType"].ToString() : "DestroyConsumers";

      OnGeneratorDestructionType onGeneratorDestructionType = (OnGeneratorDestructionType)Enum.Parse(typeof(OnGeneratorDestructionType), onGeneratorDestructionTypeRaw);

      PowerGeneratorGameLogic powerGeneratorGameLogic = GeneratorFactory.CreatePowerGenerator(this.parent, this.name, this.guid, generatorTags, powerConsumersTags, onGeneratorDestructionType);
    }
  }
}