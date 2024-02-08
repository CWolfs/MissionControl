using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using BattleTech;
using BattleTech.Framework;
using BattleTech.Designed;

using MissionControl.Data;
using MissionControl.Conditional;

using HBS.Collections;

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
        case "DialoguePageIndexMatches": BuildDialoguePageIndexMatchesConditional(conditionalObject); break;
        case "DialogueSequenceMatches": BuildDialogueSequenceMatchesConditional(conditionalObject); break;
        case "Region": BuildRegionConditional(conditionalObject); break;
        case "RegionOccupyStatus": BuildRegionOccupyStatusConditional(conditionalObject); break;
        case "EvaluateStat": BuildEvaluateStatConditional(conditionalObject); break;
        case "EvaluateTag": BuildEvaluateTagConditional(conditionalObject); break;
        case "EvaluateReflectedValue": BuildEvaluateReflectedValueConditional(conditionalObject); break;
        case "CanUnitsSeeTargetUnits": BuildCanUnitsSeeTargetUnitsConditional(conditionalObject); break;
        case "CheckTimerObjective": BuildCheckTimerObjectiveConditional(conditionalObject); break;
        case "DefendXUnitsStatus": BuildDefendXUnitsStatusConditional(conditionalObject); break;
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

    private void BuildDialoguePageIndexMatchesConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildDialoguePageIndexMatchesConditional] Building 'DialoguePageIndexMatches' conditional");
      string guid = conditionalObject["DialogueGuid"].ToString();
      int index = (int)conditionalObject["PageIndex"];

      DialoguePageIndexMatchesConditional conditional = ScriptableObject.CreateInstance<DialoguePageIndexMatchesConditional>();

      DialogueRef dialogueRef = new DialogueRef();
      dialogueRef.EncounterObjectGuid = guid;

      conditional.dialogue = dialogueRef;
      conditional.conversationIndex = index;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildDialogueSequenceMatchesConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildDialogueSequenceMatchesConditional] Building 'DialogueMatches' conditional");
      string guid = conditionalObject["DialogueSequenceGuid"].ToString();
      DialogueSequenceMatchesConditional conditional = ScriptableObject.CreateInstance<DialogueSequenceMatchesConditional>();

      DialogueSequenceRef dialogueSequenceRef = new DialogueSequenceRef();
      dialogueSequenceRef.EncounterObjectGuid = guid;

      conditional.dialogueSequence = dialogueSequenceRef;

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

    private void BuildRegionOccupyStatusConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildRegionOccupyStatusConditional] Building 'BuildRegionOccupyStatus' conditional");
      string regionGuid = conditionalObject.ContainsKey("RegionGuid") ? conditionalObject["RegionGuid"].ToString() : null;
      string lanceSpawnerGuid = conditionalObject.ContainsKey("LanceSpawnerGuid") ? conditionalObject["LanceSpawnerGuid"].ToString() : null;
      List<string> unitTags = conditionalObject.ContainsKey("UnitTags") ? conditionalObject["UnitTags"].ToObject<List<string>>() : null;
      string evaluationTypeRaw = conditionalObject["EvaluationType"].ToString();

      RegionOccupyStatusConditional conditional = ScriptableObject.CreateInstance<RegionOccupyStatusConditional>();

      if (regionGuid != null) {
        RegionRef regionRef = new RegionRef();
        regionRef.EncounterObjectGuid = regionGuid;
        conditional.region = regionRef;
      }

      if (lanceSpawnerGuid != null) {
        LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef();
        lanceSpawnerRef.EncounterObjectGuid = lanceSpawnerGuid;
        conditional.requiredLance = lanceSpawnerRef;
      }

      RegionOccupationEvaluationType evaluationType = (RegionOccupationEvaluationType)Enum.Parse(typeof(RegionOccupationEvaluationType), evaluationTypeRaw);
      conditional.regionOccupation = evaluationType;

      if (unitTags != null && unitTags.Count > 0) {
        conditional.requiredTagsOnUnit = new TagSet(unitTags.ToArray());
      } else {
        conditional.requiredTagsOnUnit = new TagSet();
      }

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildEvaluateStatConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildEvaluateStatConditional] Building 'BuildEvaluateStat' conditional");
      string scope = conditionalObject["Scope"].ToString();
      string key = conditionalObject["Key"].ToString();
      string dataType = conditionalObject["DataType"].ToString();
      string operation = conditionalObject["Operation"].ToString();
      string value = conditionalObject["Value"].ToString();

      EvaluateStatConditional conditional = ScriptableObject.CreateInstance<EvaluateStatConditional>();
      conditional.Scope = scope;
      conditional.Key = key;
      conditional.DataType = (DataType)Enum.Parse(typeof(DataType), dataType);
      conditional.Operation = (EvaluateOperation)Enum.Parse(typeof(EvaluateOperation), operation);
      conditional.Value = value;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildEvaluateTagConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildEvaluateTagConditional] Building 'BuildEvaluateTag' conditional");
      string scope = conditionalObject["Scope"].ToString();
      string operation = conditionalObject["Operation"].ToString();
      string tag = conditionalObject["Tag"].ToString();

      EvaluateTagConditional conditional = ScriptableObject.CreateInstance<EvaluateTagConditional>();
      conditional.Scope = scope;
      conditional.Operation = (ExistsOperation)Enum.Parse(typeof(ExistsOperation), operation);
      conditional.Tag = tag;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildEvaluateReflectedValueConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildEvaluateReflectedValueConditional] Building 'BuildEvaluateReflectedValue' conditional");
      string fieldToCheck = conditionalObject["Field"].ToString();
      string valueOfFieldToCheckEquality = conditionalObject["Value"].ToString();

      EvaluateReflectedValueConditional conditional = ScriptableObject.CreateInstance<EvaluateReflectedValueConditional>();
      conditional.FieldToCheck = fieldToCheck;
      conditional.ValueOfFieldToCheckEquality = valueOfFieldToCheckEquality;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildCanUnitsSeeTargetUnitsConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildCanUnitsSeeTargetUnitsConditional] Building 'CanUnitsSeeTargetUnits' conditional");
      List<string> spotterTags = conditionalObject.ContainsKey("SpotterTags") ? conditionalObject["SpotterTags"].ToObject<List<string>>() : null;
      List<string> targetTags = conditionalObject.ContainsKey("TargetTags") ? conditionalObject["TargetTags"].ToObject<List<string>>() : null;

      CanUnitsSeeTargetUnitsConditional conditional = ScriptableObject.CreateInstance<CanUnitsSeeTargetUnitsConditional>();

      if (spotterTags != null && spotterTags.Count > 0) {
        conditional.requiredTagsOnSpotterUnits = new TagSet(spotterTags.ToArray());
      } else {
        conditional.requiredTagsOnSpotterUnits = new TagSet();
      }

      if (targetTags != null && targetTags.Count > 0) {
        conditional.requiredTagsOnTargetUnits = new TagSet(targetTags.ToArray());
      } else {
        conditional.requiredTagsOnTargetUnits = new TagSet();
      }

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildCheckTimerObjectiveConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildCheckTimerObjectiveConditional] Building 'CheckTimerObjective' conditional");
      string objectiveGUID = conditionalObject["ObjectiveGuid"].ToString();
      int durationRemaining = (int)conditionalObject["DurationRemaining"];

      CheckTimerObjectiveConditional conditional = ScriptableObject.CreateInstance<CheckTimerObjectiveConditional>();

      ObjectiveRef objectiveRef = new ObjectiveRef();
      objectiveRef.EncounterObjectGuid = objectiveGUID;

      conditional.objective = objectiveRef;

      conditional.isDurationRemaining = durationRemaining;

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }

    private void BuildDefendXUnitsStatusConditional(JObject conditionalObject) {
      Main.LogDebug("[BuildDefendXUnitsStatusConditional] Building 'DefendXUnitsStatus' conditional");
      string objectiveGUID = conditionalObject["ObjectiveGuid"].ToString();
      int xOffset = (int)conditionalObject["xOffset"];
      string operation = conditionalObject["Operation"].ToString();

      DefendXUnitsObjectiveRef objectiveRef = new DefendXUnitsObjectiveRef();
      objectiveRef.EncounterObjectGuid = objectiveGUID;

      DefendXUnitsStatusConditional conditional = ScriptableObject.CreateInstance<DefendXUnitsStatusConditional>();
      conditional.defendXUnitsObjective = objectiveRef;
      conditional.relativeComparison = xOffset;
      conditional.comparisonOperator = (Operator)Enum.Parse(typeof(Operator), operation);

      conditionalList.Add(new EncounterConditionalBox(conditional));
    }
  }
}