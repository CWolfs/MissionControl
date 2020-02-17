using UnityEngine;

using Newtonsoft.Json.Linq;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using System;
using System.Collections.Generic;

using MissionControl.Result;

namespace MissionControl.ContractTypeBuilders {
  public class ResultsBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JArray resultsArray;
    private List<DesignResult> results;

    public ResultsBuilder(ContractTypeBuilder contractTypeBuilder, JArray resultsArray) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.resultsArray = resultsArray;
    }

    public List<DesignResult> Build() {
      results = new List<DesignResult>();

      foreach (JObject result in resultsArray.Children<JObject>()) {
        BuildResult(result);
      }

      return results;
    }

    private void BuildResult(JObject result) {
      string type = result["Type"].ToString();

      switch (type) {
        case "ExecuteGameLogic": BuildExecuteGameLogicResult(result); break;
        case "Dialogue": BuildDialogueGameLogicResult(result); break;
        case "SetState": BuildSetStateResult(result); break;
        case "SetStateAtRandom": BuildSetStateAtRandomResult(result); break;
        case "TagUnitsInRegion": BuildTagUnitsInRegion(result); break;
        case "SetTeamByTag": BuildSetTeamByTag(result); break;
        case "SetIsObjectiveTargetByTag": BuildSetIsObjectiveTargetByTag(result); break;
        case "SetUnitsInRegionToBeTaggedObjectives": BuildSetUnitsInRegionToBeTaggedObjectives(result); break;
        default:
          Main.Logger.LogError($"[ResultsBuilder.{contractTypeBuilder.ContractTypeKey}] No valid result was built for '{type}'");
          break;
      }
    }

    private void BuildExecuteGameLogicResult(JObject resultObject) {
      Main.LogDebug("[BuildExecuteGameLogicResult] Building 'ExecuteGameLogic' result");
      string chunkGuid = (resultObject.ContainsKey("ChunkGuid")) ? resultObject["ChunkGuid"].ToString() : null;
      string encounterGuid = (resultObject.ContainsKey("EncounterGuid")) ? resultObject["EncounterGuid"].ToString() : null;

      ExecuteGameLogicResult result = ScriptableObject.CreateInstance<ExecuteGameLogicResult>();
      result.ChunkGuid = chunkGuid;
      result.EncounterGuid = encounterGuid;

      results.Add(result);
    }

    private void BuildDialogueGameLogicResult(JObject resultObject) {
      Main.LogDebug("[BuildDialogueGameLogicResult] Building 'Dialogue' result");
      string dialogueGuid = (resultObject.ContainsKey("DialogueGuid")) ? resultObject["DialogueGuid"].ToString() : null;
      string dialogueSequenceGuid = (resultObject.ContainsKey("DialogueSequenceGuid")) ? resultObject["DialogueSequenceGuid"].ToString() : null;
      bool isInterrupt = (resultObject.ContainsKey("IsInterrupt")) ? (bool)resultObject["IsInterrupt"] : true;

      DialogResult result = ScriptableObject.CreateInstance<DialogResult>();

      DialogueRef dialogueRef = new DialogueRef();
      dialogueRef.EncounterObjectGuid = dialogueGuid;

      DialogueSequenceRef dialogueSequenceRef = new DialogueSequenceRef();
      dialogueSequenceRef.EncounterObjectGuid = dialogueSequenceGuid;

      result.dialogueRef = dialogueRef;
      result.dialogueSequenceRef = dialogueSequenceRef;
      result.isInterrupt = isInterrupt;

      results.Add(result);
    }

    private void BuildSetStateResult(JObject resultObject) {
      Main.LogDebug("[BuildSetStateResult] Building 'SetState' result");
      string encounterGuid = (resultObject.ContainsKey("EncounterGuid")) ? resultObject["EncounterGuid"].ToString() : null;
      string state = (resultObject.ContainsKey("State")) ? resultObject["State"].ToString() : null;
      EncounterObjectStatus stateType = (EncounterObjectStatus)Enum.Parse(typeof(EncounterObjectStatus), state);

      if (encounterGuid != null) {
        SetStateResult result = ScriptableObject.CreateInstance<SetStateResult>();
        result.EncounterGuid = encounterGuid;
        result.State = stateType;

        results.Add(result);
      } else {
        Main.Logger.LogError("[BuildSetStateResult] You have not provided an 'EncounterGuid' to SetState on");
      }
    }

    private void BuildSetStateAtRandomResult(JObject resultObject) {
      Main.LogDebug("[BuildSetStateAtRandomResult] Building 'SetStateAtRandom' result");
      List<string> encounterGuids = (resultObject.ContainsKey("EncounterGuids")) ? resultObject["EncounterGuids"].ToObject<List<string>>() : null;
      string state = (resultObject.ContainsKey("State")) ? resultObject["State"].ToString() : null;
      EncounterObjectStatus stateType = (EncounterObjectStatus)Enum.Parse(typeof(EncounterObjectStatus), state);

      if (encounterGuids != null) {
        SetStateAtRandomResult result = ScriptableObject.CreateInstance<SetStateAtRandomResult>();
        result.EncounterGuids = encounterGuids;
        result.State = stateType;

        results.Add(result);
      } else {
        Main.Logger.LogError("[BuildSetStateAtRandomResult] You have not provided an 'EncounterGuids' to SetState on");
      }
    }

    private void BuildTagUnitsInRegion(JObject resultObject) {
      Main.LogDebug("[BuildTagUnitsInRegion] Building 'TagXUnitsInRegion' result");
      string regionGuid = resultObject["RegionGuid"].ToString();
      string unitType = resultObject["UnitType"].ToString();
      int numberOfUnits = (int)resultObject["NumberOfUnits"];
      string[] tags = ((JArray)resultObject["Tags"]).ToObject<string[]>();

      if (regionGuid != null) {
        TagUnitsInRegionResult result = ScriptableObject.CreateInstance<TagUnitsInRegionResult>();
        result.RegionGuid = regionGuid;
        result.Type = unitType;
        result.NumberOfUnits = numberOfUnits;
        result.Tags = tags;

        results.Add(result);
      } else {
        Main.Logger.LogError("[BuildTagUnitsInRegion] You have not provided an 'RegionGuid' to BuildTagUnitsInRegion on");
      }
    }

    private void BuildSetTeamByTag(JObject resultObject) {
      Main.LogDebug("[BuildSetTeamByTag] Building 'SetTeamByTag' result");
      string team = resultObject["Team"].ToString();
      string[] tags = ((JArray)resultObject["Tags"]).ToObject<string[]>();
      string[] applyTags = resultObject.ContainsKey("ApplyTags") ? ((JArray)resultObject["Tags"]).ToObject<string[]>() : null;

      SetTeamByTagResult result = ScriptableObject.CreateInstance<SetTeamByTagResult>();
      result.Team = team;
      result.Tags = tags;
      if (applyTags != null) result.ApplyTags = applyTags;

      results.Add(result);
    }

    private void BuildSetIsObjectiveTargetByTag(JObject resultObject) {
      Main.LogDebug("[BuildSetIsObjectiveTargetByTag] Building 'SetIsObjectiveTargetByTag' result");
      bool isObjectiveTarget = (bool)resultObject["IsObjectiveTarget"];
      string[] tags = ((JArray)resultObject["Tags"]).ToObject<string[]>();

      SetIsObjectiveTargetByTagResult result = ScriptableObject.CreateInstance<SetIsObjectiveTargetByTagResult>();
      result.IsObjectiveTarget = isObjectiveTarget;
      result.Tags = tags;

      results.Add(result);
    }

    private void BuildSetUnitsInRegionToBeTaggedObjectives(JObject resultObject) {
      Main.LogDebug("[BuildSetUnitsInRegionToBeTaggedObjectives] Building 'SetUnitsInRegionToBeTaggedObjectives' result");
      string regionGuid = resultObject["RegionGuid"].ToString();
      string unitType = resultObject["UnitType"].ToString();
      int numberOfUnits = (int)resultObject["NumberOfUnits"];
      string team = resultObject["Team"].ToString();
      bool isObjectiveTarget = (bool)resultObject["IsObjectiveTarget"];
      string[] tags = ((JArray)resultObject["Tags"]).ToObject<string[]>();

      if (regionGuid != null) {
        SetUnitsInRegionToBeTaggedObjectivesResult result = ScriptableObject.CreateInstance<SetUnitsInRegionToBeTaggedObjectivesResult>();
        result.RegionGuid = regionGuid;
        result.Type = unitType;
        result.NumberOfUnits = numberOfUnits;
        result.Team = team;
        result.IsObjectiveTarget = isObjectiveTarget;
        result.Tags = tags;

        results.Add(result);
      } else {
        Main.Logger.LogError("[BuildSetUnitsInRegionToBeTaggedObjectives] You have not provided an 'RegionGuid' to BuildTagUnitsInRegion on");
      }
    }
  }
}