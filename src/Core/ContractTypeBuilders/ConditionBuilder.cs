using UnityEngine;

using System;

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
        case "ObjectiveStatusConditional": return BuildObjectiveStatusConditional(conditionalObject);
        default: break;
      }

      Main.Logger.LogError($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] No valid conditional was built for '{type}'");

      return null;
    }

    private DesignConditional BuildAlwaysTrueConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildAlwaysTrueConditional] Building 'AlwaysTrueConditional' conditional");
      return ScriptableObject.CreateInstance<AlwaysTrueConditional>();
    }

    private DesignConditional BuildObjectiveStatusConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildAlwaysTrueConditional] Building 'ObjectiveStatusConditional' conditional");
      string guid = conditionalObject["Guid"].ToString();
      string status = conditionalObject["Status"].ToString();
      ObjectiveStatusEvaluationType statusType = (ObjectiveStatusEvaluationType)Enum.Parse(typeof(ObjectiveStatusEvaluationType), status);
      ObjectiveStatusConditional conditional = ScriptableObject.CreateInstance<ObjectiveStatusConditional>();

      ObjectiveRef objectiveRef = new ObjectiveRef();
      objectiveRef.EncounterObjectGuid = guid;

      conditional.objective = objectiveRef;
      conditional.objectiveStatus = statusType;

      return conditional;
    }
  }
}