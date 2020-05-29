using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using BattleTech;
using BattleTech.Framework;
using BattleTech.Designed;

using MissionControl.Conditional;

namespace MissionControl.ContractTypeBuilders {
  public class ConditionalBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JArray conditionalsObject;
    private GenericCompoundConditional conditionals;
    private List<EncounterConditionalBox> conditionalList;

    public ConditionalBuilder(ContractTypeBuilder contractTypeBuilder, JArray conditionalsObject) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.conditionalsObject = conditionalsObject;
    }

    public GenericCompoundConditional Build() {
      conditionalList = new List<EncounterConditionalBox>();
      conditionals = ScriptableObject.CreateInstance<GenericCompoundConditional>();

      foreach (JObject conditionalObject in conditionalsObject.Children<JObject>()) {
        BuildConditional(conditionalObject);
      }

      conditionals.conditionalList = conditionalList.ToArray();
      return conditionals;
    }

    public List<DesignConditional> BuildAsDesignConditionals() {
      conditionalList = new List<EncounterConditionalBox>();

      foreach (JObject conditionalObject in conditionalsObject.Children<JObject>()) {
        BuildConditional(conditionalObject);
      }

      return conditionalList.Select(conditional => conditional.CargoVTwo).ToList();
    }

    private void BuildConditional(JObject conditionalObject) {
      string type = conditionalObject["Type"].ToString();
      string contractTypeKey = (this.contractTypeBuilder != null) ? contractTypeBuilder.ContractTypeKey : "Result";

      switch (type) {
        case "AlwaysTrueConditional": BuildAlwaysTrueConditional(conditionalObject); break;
        case "ObjectiveStatusConditional": BuildObjectiveStatusConditional(conditionalObject); break;
        case "ObjectiveStatusesConditional": BuildObjectStatusesConditional(conditionalObject); break;
        case "EncounterObjectMatchesStateConditional": BuildEncounterObjectMatchesStateConditional(conditionalObject); break;
        case "DialogueMatchesConditional": BuildDialogueMatchesConditional(conditionalObject); break;
        default:
          Main.Logger.LogError($"[ChunkTypeBuilder.{contractTypeKey}] No valid conditional was built for '{type}'");
          break;
      }
    }

    private void BuildAlwaysTrueConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildAlwaysTrueConditional] Building 'AlwaysTrueConditional' conditional");
      AlwaysTrueConditional conditional = ScriptableObject.CreateInstance<AlwaysTrueConditional>();
      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildObjectiveStatusConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildObjectiveStatusConditional] Building 'ObjectiveStatusConditional' conditional");
      string guid = conditionalObject["Guid"].ToString();
      string status = conditionalObject["Status"].ToString();
      ObjectiveStatusEvaluationType statusType = (ObjectiveStatusEvaluationType)Enum.Parse(typeof(ObjectiveStatusEvaluationType), status);
      ObjectiveStatusConditional conditional = ScriptableObject.CreateInstance<ObjectiveStatusConditional>();

      ObjectiveRef objectiveRef = new ObjectiveRef();
      objectiveRef.EncounterObjectGuid = guid;

      conditional.objective = objectiveRef;
      conditional.objectiveStatus = statusType;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildObjectStatusesConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildObjectStatusesConditional] Building 'ObjectStatusesConditional' conditional");
      bool notInContractObjectivesAreSuccesses = conditionalObject.ContainsKey("NotInContractObjectivesAreSuccesses") ? (bool)conditionalObject["NotInContractObjectivesAreSuccesses"] : true;
      JArray statusesArray = (JArray)conditionalObject["Statuses"];
      Dictionary<string, ObjectiveStatusEvaluationType> statuses = new Dictionary<string, ObjectiveStatusEvaluationType>();

      foreach (JObject status in statusesArray.Children<JObject>()) {
        string childGuid = status["Guid"].ToString();
        string childStatus = status["Status"].ToString();
        ObjectiveStatusEvaluationType childStatusType = (ObjectiveStatusEvaluationType)Enum.Parse(typeof(ObjectiveStatusEvaluationType), childStatus);
        statuses.Add(childGuid, childStatusType);
      }

      ObjectiveStatusesConditional conditional = ScriptableObject.CreateInstance<ObjectiveStatusesConditional>();
      conditional.NotInContractObjectivesAreSuccesses = notInContractObjectivesAreSuccesses;
      conditional.Statuses = statuses;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildEncounterObjectMatchesStateConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildEncounterObjectMatchesStateConditional] Building 'EncounterObjectMatchesStateConditional' conditional");
      string guid = conditionalObject["Guid"].ToString();
      string status = conditionalObject["Status"].ToString();
      EncounterObjectStatus statusType = (EncounterObjectStatus)Enum.Parse(typeof(EncounterObjectStatus), status);
      EncounterObjectMatchesStateConditional conditional = ScriptableObject.CreateInstance<EncounterObjectMatchesStateConditional>();

      conditional.EncounterGuid = guid;
      conditional.State = statusType;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildDialogueMatchesConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildDialogueMatchesConditional] Building 'DialogueMatchesConditional' conditional");
      string guid = conditionalObject["DialogueGuid"].ToString();
      DialogueMatchesConditional conditional = ScriptableObject.CreateInstance<DialogueMatchesConditional>();

      DialogueRef dialogueRef = new DialogueRef();
      dialogueRef.EncounterObjectGuid = guid;

      conditional.dialogue = dialogueRef;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }
  }
}