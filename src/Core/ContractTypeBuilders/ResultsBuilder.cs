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

    private void WarnOfDeprecation(string resultName) {
      Main.Logger.LogWarning($"[ResultsBuilder] DEPRECATION WARNING: Result {resultName} is deprecated. Read the changelog or documentation for updated usage. THIS RESULT TYPE WILL STOP WORKING IN A FUTURE RELEASE.");
    }

    private void BuildResult(JObject result) {
      string type = result["Type"].ToString();

      // update deprecated result types
      switch (type) {
        case "SetLanceEvasionTicksByTag": WarnOfDeprecation(type); type = "SetUnitEvasionTicksByTag"; break; // DEPRECATION VERSION: 1
        case "ModifyLanceEvasionTicksByTag": WarnOfDeprecation(type); type = "ModifyUnitEvasionTicksByTag"; break; // DEPRECATION VERSION: 1
        case "SetStateAtRandom": WarnOfDeprecation(type); type = "SetStatusAtRandom"; break; // DEPRECATION VERSION: 1
      }
      // end update

      switch (type) {
        case "ExecuteGameLogic": BuildExecuteGameLogicResult(result); break;
        case "Dialogue": BuildDialogueGameLogicResult(result); break;
        case "SetState": BuildSetStateResult(result); break;
        case "SetStatusAtRandom": BuildSetStatusAtRandomResult(result); break;
        case "TagUnitsInRegion": BuildTagUnitsInRegion(result); break;
        case "SetTeamByTag": BuildSetTeamByTag(result); break;
        case "SetTeamByLanceSpawnerGuid": BuildSetTeamByLanceSpawnerGuid(result); break;
        case "SetIsObjectiveTargetByTag": BuildSetIsObjectiveTargetByTag(result); break;
        case "SetUnitsInRegionToBeTaggedObjectiveTargets": BuildSetUnitsInRegionToBeTaggedObjectiveTargetsResult(result); break;
        case "CompleteObjective": BuildCompleteObjectiveResult(result); break;
        case "SetTemporaryUnitPhaseInitiativeByTag": BuildSetTemporaryUnitPhaseInitiativeByTagResult(result); break;
        case "SetUnitEvasionTicksByTag": BuildSetUnitEvasionTicksByTagResult(result); break;
        case "ModifyUnitEvasionTicksByTag": BuildModifyUnitEvasionTicksByTagResult(result); break;
        case "CameraFocus": BuildCameraFocusResult(result); break;
        case "DestroyBuildingsAtLanceSpawns": BuildDestroyBuildingsAtLanceSpawnsResult(result); break;
        case "Delay": BuildDelayResult(result); break;
        case "IgnoreChunks": BuildIgnoreChunksResult(result); break;
        case "TriggerResultAtRandom": BuildTriggerResultAtRandomResult(result); break;
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

    private void BuildSetStatusAtRandomResult(JObject resultObject) {
      Main.LogDebug("[BuildSetStatusAtRandomResult] Building 'SetStatusAtRandom' result");
      List<string> encounterGuids = (resultObject.ContainsKey("EncounterGuids")) ? resultObject["EncounterGuids"].ToObject<List<string>>() : null;
      string status = "Active";

      if (resultObject.ContainsKey("Status")) {
        status = resultObject["Status"].ToString();
      } else if (resultObject.ContainsKey("State")) {
        status = resultObject["State"].ToString();
      }

      EncounterObjectStatus statusType = (EncounterObjectStatus)Enum.Parse(typeof(EncounterObjectStatus), status);

      if (encounterGuids != null) {
        SetStatusAtRandomResult result = ScriptableObject.CreateInstance<SetStatusAtRandomResult>();
        result.EncounterGuids = encounterGuids;
        result.Status = statusType;

        results.Add(result);
      } else {
        Main.Logger.LogError("[BuildSetStatusAtRandomResult] You have not provided an 'EncounterGuids' to SetStatus on");
      }
    }

    private void BuildTagUnitsInRegion(JObject resultObject) {
      Main.LogDebug("[BuildTagUnitsInRegion] Building 'TagXUnitsInRegion' result");
      string regionGuid = resultObject["RegionGuid"].ToString();
      string unitType = resultObject["UnitType"].ToString();
      int numberOfUnits = resultObject.ContainsKey("NumberOfUnits") ? (int)resultObject["NumberOfUnits"] : 0;
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
      bool alertLance = resultObject.ContainsKey("AlertLance") ? (bool)resultObject["AlertLance"] : true;
      string[] applyTags = resultObject.ContainsKey("ApplyTags") ? ((JArray)resultObject["Tags"]).ToObject<string[]>() : null;

      SetTeamByTagResult result = ScriptableObject.CreateInstance<SetTeamByTagResult>();
      result.Team = team;
      result.Tags = tags;
      result.AlertLance = alertLance;
      if (applyTags != null) result.ApplyTags = applyTags;

      results.Add(result);
    }

    private void BuildSetTeamByLanceSpawnerGuid(JObject resultObject) {
      Main.LogDebug("[BuildSetTeamByLanceSpawnerGuid] Building 'SetTeamByLanceSpawnerGuid' result");
      string team = resultObject["Team"].ToString();
      string lanceSpawnerGuid = resultObject["LanceSpawnerGuid"].ToString();
      bool alertLance = resultObject.ContainsKey("AlertLance") ? (bool)resultObject["AlertLance"] : true;
      string[] applyTags = resultObject.ContainsKey("ApplyTags") ? ((JArray)resultObject["Tags"]).ToObject<string[]>() : null;

      SetTeamByLanceSpawnerGuid result = ScriptableObject.CreateInstance<SetTeamByLanceSpawnerGuid>();
      result.Team = team;
      result.LanceSpawnerGuid = lanceSpawnerGuid;
      result.AlertLance = alertLance;
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

    private void BuildSetUnitsInRegionToBeTaggedObjectiveTargetsResult(JObject resultObject) {
      Main.LogDebug("[BuildSetUnitsInRegionToBeTaggedObjectiveTargetsResult] Building 'SetUnitsInRegionToBeTaggedObjectiveTargets' result");
      string regionGuid = resultObject["RegionGuid"].ToString();
      string unitType = resultObject["UnitType"].ToString();
      int numberOfUnits = resultObject.ContainsKey("NumberOfUnits") ? (int)resultObject["NumberOfUnits"] : 0;
      string team = resultObject["Team"].ToString();
      bool isObjectiveTarget = (bool)resultObject["IsObjectiveTarget"];
      string[] tags = ((JArray)resultObject["Tags"]).ToObject<string[]>();

      if (regionGuid != null) {
        SetUnitsInRegionToBeTaggedObjectiveTargetsResult result = ScriptableObject.CreateInstance<SetUnitsInRegionToBeTaggedObjectiveTargetsResult>();
        result.RegionGuid = regionGuid;
        result.Type = unitType;
        result.NumberOfUnits = numberOfUnits;
        result.Team = team;
        result.IsObjectiveTarget = isObjectiveTarget;
        result.Tags = tags;

        results.Add(result);
      } else {
        Main.Logger.LogError("[SetUnitsInRegionToBeTaggedObjectiveTargetsResult] You have not provided an 'RegionGuid' to TagUnitsInRegion on");
      }
    }

    private void BuildCompleteObjectiveResult(JObject resultObject) {
      Main.LogDebug("[BuildCompleteObjectiveResult] Building 'CompleteObjectiveResult' result");
      string objectiveGuid = resultObject["ObjectiveGuid"].ToString();
      string completionTypeStr = (resultObject.ContainsKey("CompletionType")) ? resultObject["CompletionType"].ToString() : "Succeed";
      CompleteObjectiveType completionType = (CompleteObjectiveType)Enum.Parse(typeof(CompleteObjectiveType), completionTypeStr);

      if (objectiveGuid != null) {
        CompleteObjectiveResult result = ScriptableObject.CreateInstance<CompleteObjectiveResult>();
        ObjectiveRef objectiveRef = new ObjectiveRef();
        objectiveRef.EncounterObjectGuid = objectiveGuid;
        result.objectiveRef = objectiveRef;
        result.completeObjectiveType = completionType;

        results.Add(result);
      } else {
        Main.Logger.LogError("[BuildCompleteObjectiveResult] You have not provided an 'ObjectiveGuid' to CompleteObjectiveResult on");
      }
    }

    private void BuildSetTemporaryUnitPhaseInitiativeByTagResult(JObject resultObject) {
      Main.LogDebug("[BuildSetTemporaryUnitPhaseInitiativeByTagResult] Building 'SetTemporaryUnitPhaseInitiativeByTag' result");
      int initiative = (int)resultObject["Initiative"];
      string[] tags = ((JArray)resultObject["Tags"]).ToObject<string[]>();

      SetTemporaryUnitPhaseInitiativeByTagResult result = ScriptableObject.CreateInstance<SetTemporaryUnitPhaseInitiativeByTagResult>();
      result.Initiative = initiative;
      result.Tags = tags;

      results.Add(result);
    }

    private void BuildSetUnitEvasionTicksByTagResult(JObject resultObject) {
      Main.LogDebug("[BuildSetUnitEvasionTicksByTagResult] Building 'SetUnitEvasionTicksByTag' result");
      int amount = (int)resultObject["Amount"];
      string[] tags = ((JArray)resultObject["Tags"]).ToObject<string[]>();

      SetUnitEvasionTicksByTagResult result = ScriptableObject.CreateInstance<SetUnitEvasionTicksByTagResult>();
      result.Amount = amount;
      result.Tags = tags;

      results.Add(result);
    }

    private void BuildModifyUnitEvasionTicksByTagResult(JObject resultObject) {
      Main.LogDebug("[BuildModifyUnitEvasionTicksByTagResult] Building 'ModifyUnitEvasionTicksByTag' result");
      int amount = (int)resultObject["Amount"];
      string[] tags = ((JArray)resultObject["Tags"]).ToObject<string[]>();

      ModifyUnitEvasionTicksByTagResult result = ScriptableObject.CreateInstance<ModifyUnitEvasionTicksByTagResult>();
      result.Amount = amount;
      result.Tags = tags;

      results.Add(result);
    }

    private void BuildCameraFocusResult(JObject resultObject) {
      Main.LogDebug("[BuildCameraFocusResult] Building 'CameraFocus' result");
      string guid = resultObject["EncounterGuid"].ToString();
      string distanceStr = resultObject.ContainsKey("Distance") ? resultObject["Distance"].ToString() : "Medium";
      string heightStr = resultObject.ContainsKey("Height") ? resultObject["Height"].ToString() : "Default";
      float focusTime = resultObject.ContainsKey("FocusTime") ? (float)resultObject["FocusTime"] : -1;
      float focusRadius = resultObject.ContainsKey("FocusRadius") ? (float)resultObject["FocusRadius"] : -1;
      bool isInterrupt = resultObject.ContainsKey("IsInterrupt") ? (bool)resultObject["IsInterrupt"] : true;

      DialogCameraDistance distance = (DialogCameraDistance)Enum.Parse(typeof(DialogCameraDistance), distanceStr);
      DialogCameraHeight height = (DialogCameraHeight)Enum.Parse(typeof(DialogCameraHeight), heightStr);

      CameraResult result = ScriptableObject.CreateInstance<CameraResult>();

      EncounterObjectRef encounterObjectRef = new EncounterObjectRef();
      encounterObjectRef.EncounterObjectGuid = guid;

      result.cameraFocus = encounterObjectRef;
      result.cameraDistance = distance;
      result.cameraHeight = height;
      result.cameraFocusTime = focusTime;
      result.cameraFocusRadius = focusRadius;
      result.isInterrupt = isInterrupt;

      results.Add(result);
    }

    private void BuildDestroyBuildingsAtLanceSpawnsResult(JObject resultObject) {
      Main.LogDebug("[BuildDestroyBuildingsAtLanceSpawnsResult] Building 'DestroyBuildingsAtLanceSpawns' result");
      string guid = resultObject["LanceSpawnerGuid"].ToString();
      float radius = resultObject.ContainsKey("Radius") ? (float)resultObject["Radius"] : 48;

      DestroyBuildingsAtLanceSpawnsResult result = ScriptableObject.CreateInstance<DestroyBuildingsAtLanceSpawnsResult>();
      result.LanceSpawnerGuid = guid;
      result.Radius = radius;

      results.Add(result);
    }

    private void BuildDelayResult(JObject resultObject) {
      Main.LogDebug("[BuildDelayResult] Building 'Delay' result");
      string name = resultObject.ContainsKey("Name") ? resultObject["Name"].ToString() : "Unnamed Delay Result";
      float time = resultObject.ContainsKey("Time") ? (float)resultObject["Time"] : -1;
      int rounds = resultObject.ContainsKey("Rounds") ? (int)resultObject["Rounds"] : -1;
      int phases = resultObject.ContainsKey("Phases") ? (int)resultObject["Phases"] : -1;
      JObject skipIfTrigger = resultObject.ContainsKey("SkipIf") ? (JObject)resultObject["SkipIf"] : null;
      List<DesignConditional> childSkipIfConditionals = new List<DesignConditional>();

      JArray childResultsArray = (JArray)resultObject["Results"];
      JArray childResultsIfSkippedArray = resultObject.ContainsKey("ResultsIfSkipped") ? (JArray)resultObject["ResultsIfSkipped"] : null;

      List<DesignResult> createdChildResults = new List<DesignResult>();
      ResultsBuilder childResultsBuilder = new ResultsBuilder(this.contractTypeBuilder, childResultsArray);
      createdChildResults = childResultsBuilder.Build();

      List<DesignResult> createdChildResultsIfSkipped = new List<DesignResult>();
      if (childResultsIfSkippedArray != null) {
        ResultsBuilder childResultsIfSkippedBuilder = new ResultsBuilder(this.contractTypeBuilder, childResultsIfSkippedArray);
        createdChildResultsIfSkipped = childResultsIfSkippedBuilder.Build();
      }

      DelayResult result = ScriptableObject.CreateInstance<DelayResult>();
      result.Name = name;
      result.Time = time;
      result.Rounds = rounds;
      result.Phases = phases;
      result.Results = createdChildResults;
      result.ResultsIfSkipped = createdChildResultsIfSkipped;

      results.Add(result);

      if (skipIfTrigger != null) {
        GenericTriggerBuilder genericTrigger = new GenericTriggerBuilder(this.contractTypeBuilder, skipIfTrigger, "DelaySkipIfTrigger");
        ActivateDelaySkipResultsResult triggerResult = ScriptableObject.CreateInstance<ActivateDelaySkipResultsResult>();
        triggerResult.DelayResult = result;
        genericTrigger.Results = new List<DesignResult>() { triggerResult };
        genericTrigger.Build();
      }
    }

    private void BuildIgnoreChunksResult(JObject resultObject) {
      Main.LogDebug("[BuildIgnoreChunksResult] Building 'IgnoreChunks' result");
      List<string> encounterGuids = (resultObject.ContainsKey("EncounterGuids")) ? resultObject["EncounterGuids"].ToObject<List<string>>() : null;

      if (encounterGuids != null) {
        IgnoreChunksResult result = ScriptableObject.CreateInstance<IgnoreChunksResult>();
        result.EncounterGuids = encounterGuids;

        results.Add(result);
      } else {
        Main.Logger.LogError("[BuildIgnoreChunksResult] You have not provided an 'EncounterGuids' to Ignore");
      }
    }

    private void BuildTriggerResultAtRandomResult(JObject resultObject) {
      Main.LogDebug("[BuildTriggerResultAtRandomResult] Building 'TriggerResultAtRandom' result");
      JArray childResultsArray = (JArray)resultObject["Results"];

      List<DesignResult> createdChildResults = new List<DesignResult>();
      ResultsBuilder childResultsBuilder = new ResultsBuilder(this.contractTypeBuilder, childResultsArray);
      createdChildResults = childResultsBuilder.Build();

      TriggerResultAtRandomResult result = ScriptableObject.CreateInstance<TriggerResultAtRandomResult>();
      result.Results = createdChildResults;

      results.Add(result);
    }
  }
}