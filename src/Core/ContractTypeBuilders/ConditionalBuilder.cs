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
      // v2 upgrade path: Remove 'Conditional' from type of Conditionals.
      if (type.EndsWith("Conditional")) type = type.Substring(0, type.LastIndexOf("Conditional"));

      switch (type) {
        case "AlwaysTrue": BuildAlwaysTrueConditional(conditionalObject); break;
        case "ObjectiveStatus": BuildObjectiveStatusConditional(conditionalObject); break;
        case "ObjectiveStatuses": BuildObjectStatusesConditional(conditionalObject); break;
        case "EncounterObjectMatchesState": BuildEncounterObjectMatchesStateConditional(conditionalObject); break;
        case "DialogueMatches": BuildDialogueMatchesConditional(conditionalObject); break;
        case "Region": BuildRegionConditional(conditionalObject); break;
        default:
          Main.Logger.LogError($"[ChunkTypeBuilder.{contractTypeKey}] No valid conditional was built for '{type}'");
          break;
      }
    }

    private void BuildAlwaysTrueConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildAlwaysTrueConditional] Building 'AlwaysTrue' conditional");
      AlwaysTrueConditional conditional = ScriptableObject.CreateInstance<AlwaysTrueConditional>();
      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildObjectiveStatusConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildObjectiveStatusConditional] Building 'ObjectiveStatus' conditional");
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
      Main.LogDebug("[BuildObjectStatusesConditional] Building 'ObjectStatuses' conditional");
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
      Main.LogDebug("[BuildEncounterObjectMatchesStateConditional] Building 'EncounterObjectMatchesState' conditional");
      string guid = conditionalObject["Guid"].ToString();
      string status = conditionalObject["Status"].ToString();
      EncounterObjectStatus statusType = (EncounterObjectStatus)Enum.Parse(typeof(EncounterObjectStatus), status);
      EncounterObjectMatchesStateConditional conditional = ScriptableObject.CreateInstance<EncounterObjectMatchesStateConditional>();

      conditional.EncounterGuid = guid;
      conditional.State = statusType;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildDialogueMatchesConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildDialogueMatchesConditional] Building 'DialogueMatches' conditional");
      string guid = conditionalObject["DialogueGuid"].ToString();
      DialogueMatchesConditional conditional = ScriptableObject.CreateInstance<DialogueMatchesConditional>();

      DialogueRef dialogueRef = new DialogueRef();
      dialogueRef.EncounterObjectGuid = guid;

      conditional.dialogue = dialogueRef;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildRegionConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildRegionConditional] Building 'BuildRegion' conditional");
      string regionGuid = conditionalObject.ContainsKey("RegionGuid") ? conditionalObject["RegionGuid"].ToString() : null;
      string movementTypeRaw = conditionalObject["MovementType"].ToString();
      List<string> regionTags = conditionalObject.ContainsKey("RegionTags") ? conditionalObject["RegionTags"].ToObject<List<string>>() : null;
      List<string> unitTags = conditionalObject.ContainsKey("UnitTags") ? conditionalObject["UnitTags"].ToObject<List<string>>() : null;

      RegionConditional conditional = ScriptableObject.CreateInstance<RegionConditional>();

      if (regionGuid != null) {
        RegionRef regionRef = new RegionRef();
        regionRef.EncounterObjectGuid = regionGuid;
        conditional.exactRegion = regionRef;
      }

      RegionMovementType movementType = (RegionMovementType)Enum.Parse(typeof(RegionMovementType), movementTypeRaw);
      conditional.regionMovementType = movementType;

      conditional.regionTagSet.AddRange(regionTags);
      conditional.actingUnitTagSet.AddRange(unitTags);

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }
  }
}