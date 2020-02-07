using UnityEngine;

using Newtonsoft.Json.Linq;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.ContractTypeBuilders {
  public class ConditionalBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject conditionalObject;

    public ConditionalBuilder(ContractTypeBuilder contractTypeBuilder, JObject conditionalObject) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.conditionalObject = conditionalObject;
    }

    public DesignConditional Build() {
      string type = conditionalObject["Type"].ToString();

      switch (type) {
        case "AlwaysTrueConditional": return BuildAlwaysTrueConditional(conditionalObject);
        default: break;
      }

      Main.Logger.LogError($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] No valid conditional was built for '{type}'");

      return null;
    }

    private DesignConditional BuildAlwaysTrueConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildAlwaysTrueConditional] Building BuildAlwaysTrue conditional");
      return ScriptableObject.CreateInstance<AlwaysTrueConditional>();
    }
  }
}