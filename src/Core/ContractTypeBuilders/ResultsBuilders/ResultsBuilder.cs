using UnityEngine;

using Newtonsoft.Json.Linq;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using System;
using System.Collections.Generic;

using MissionControl.Result;
using MissionControl.Data;

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
        case "SetState": WarnOfDeprecation(type); type = "SetStatus"; break; // DEPRECATION VERSION: 1
      }
      // end update

      switch (type) {
        case "ExecuteGameLogic": BuildExecuteResult(result); break;
        case "Dialogue": BuildDialogueResult(result); break;
        case "DialogueSequence": BuildDialogueSequenceResult(result); break;
        case "SetStatus": BuildSetStatusResult(result); break;
        case "SetStatusAtRandom": BuildSetStatusAtRandomResult(result); break;
        case "TagUnitsInRegion": BuildTagUnitsInRegionResult(result); break;
        case "SetTeamByTag": BuildSetTeamByTagResult(result); break;
        case "SetTeamByLanceSpawnerGuid": BuildSetTeamByLanceSpawnerGuidResult(result); break;
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
        case "DebugStatLogger": BuildDebugStatLoggerResult(result); break;
        case "DebugTagLogger": BuildDebugTagLoggerResult(result); break;
        case "SetStat": BuildSetStatResult(result); break;
        case "SetTag": BuildSetTagResult(result); break;
        case "SetAIPatrolRoute": BuildSetAIPatrolRouteResult(result); break;
        case "SetAIBehaviourTree": BuildSetAIBehaviourTreeResult(result); break;
        case "SwapTeams": BuildSwapTeamsResult(result); break;
        case "SetAllTeamsRelationship": BuildSetAllTeamsRelationshipResult(result); break;
        default:
          Main.Logger.LogError($"[ResultsBuilder.{contractTypeBuilder.ContractTypeKey}] No valid result was built for '{type}'");
          break;
      }
    }

    private void BuildExecuteResult(JObject resultObject) {
      Main.LogDebug("[BuildExecuteResult] Building 'ExecuteGameLogic' result");
      string chunkGuid = (resultObject.ContainsKey("ChunkGuid")) ? resultObject["ChunkGuid"].ToString() : null;
      string encounterGuid = (resultObject.ContainsKey("EncounterGuid")) ? resultObject["EncounterGuid"].ToString() : null;

      ExecuteGameLogicResult result = ScriptableObject.CreateInstance<ExecuteGameLogicResult>();
      result.ChunkGuid = chunkGuid;
      result.EncounterGuid = encounterGuid;

      results.Add(result);
    }

    private void BuildDialogueResult(JObject resultObject) {
      Main.LogDebug("[BuildDialogueResult] Building 'Dialogue' result");
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

    private void BuildDialogueSequenceResult(JObject resultObject) {
      Main.LogDebug("[BuildDialogueSequenceResult] Building 'DialogueSequence' result");
      string dialogueSequenceGuid = (resultObject.ContainsKey("DialogueSequenceGuid")) ? resultObject["DialogueSequenceGuid"].ToString() : null;

      DialogResult result = ScriptableObject.CreateInstance<DialogResult>();

      DialogueSequenceRef dialogueSequenceRef = new DialogueSequenceRef();
      dialogueSequenceRef.EncounterObjectGuid = dialogueSequenceGuid;

      result.dialogueSequenceRef = dialogueSequenceRef;

      results.Add(result);
    }

    private void BuildSetStatusResult(JObject resultObject) {
      Main.LogDebug("[BuildSetStatusResult] Building 'SetStatus' result");
      string encounterGuid = (resultObject.ContainsKey("EncounterGuid")) ? resultObject["EncounterGuid"].ToString() : null;

      string status = "Active";
      if (resultObject.ContainsKey("Status")) {
        status = resultObject["Status"].ToString();
      } else if (resultObject.ContainsKey("State")) {
        status = resultObject["State"].ToString();
      }

      EncounterObjectStatus statusType = (EncounterObjectStatus)Enum.Parse(typeof(EncounterObjectStatus), status);

      if (encounterGuid != null) {
        SetStatusResult result = ScriptableObject.CreateInstance<SetStatusResult>();
        result.EncounterGuid = encounterGuid;
        result.Status = statusType;

        results.Add(result);
      } else {
        Main.Logger.LogError("[BuildSetStatusResult] You have not provided an 'EncounterGuid' to SetStatus on");
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

    private void BuildTagUnitsInRegionResult(JObject resultObject) {
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

    private void BuildSetTeamByTagResult(JObject resultObject) {
      Main.LogDebug("[BuildSetTeamByTag] Building 'SetTeamByTag' result");
      string team = resultObject["Team"].ToString();
      string[] tags = ((JArray)resultObject["Tags"]).ToObject<string[]>();
      bool alertLance = resultObject.ContainsKey("AlertLance") ? (bool)resultObject["AlertLance"] : true;
      string[] applyTags = resultObject.ContainsKey("ApplyTags") ? ((JArray)resultObject["ApplyTags"]).ToObject<string[]>() : null;

      SetTeamByTagResult result = ScriptableObject.CreateInstance<SetTeamByTagResult>();
      result.Team = team;
      result.Tags = tags;
      result.AlertLance = alertLance;
      if (applyTags != null) result.ApplyTags = applyTags;

      results.Add(result);
    }

    private void BuildSetTeamByLanceSpawnerGuidResult(JObject resultObject) {
      Main.LogDebug("[BuildSetTeamByLanceSpawnerGuid] Building 'SetTeamByLanceSpawnerGuid' result");
      string team = resultObject["Team"].ToString();
      string lanceSpawnerGuid = resultObject["LanceSpawnerGuid"].ToString();
      bool alertLance = resultObject.ContainsKey("AlertLance") ? (bool)resultObject["AlertLance"] : true;
      string[] applyTags = resultObject.ContainsKey("ApplyTags") ? ((JArray)resultObject["ApplyTags"]).ToObject<string[]>() : null;

      SetTeamByLanceSpawnerGuidResult result = ScriptableObject.CreateInstance<SetTeamByLanceSpawnerGuidResult>();
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

      // Skip If Trigger Type & Execution
      JObject skipIfTrigger = resultObject.ContainsKey("SkipIf") ? (JObject)resultObject["SkipIf"] : null;
      string skipIfType = "WatchFromContractStart";
      string skipIfExecution = "ImmediateOnSuccess";

      if (skipIfTrigger != null) {
        skipIfType = skipIfTrigger.ContainsKey("Type") ? skipIfTrigger["Type"].ToString() : "WatchFromContractStart";
        skipIfExecution = skipIfTrigger.ContainsKey("Execution") ? skipIfTrigger["Execution"].ToString() : "ImmediateOnSuccess";
      }

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
      result.SkipIfExecution = skipIfExecution;

      // Build SkipIf triggers
      if (skipIfTrigger.ContainsKey("Triggers")) {
        JArray childSkipIfTriggersArray = (JArray)skipIfTrigger["Triggers"];
        foreach (JObject skipIfTriggerObject in childSkipIfTriggersArray.Children<JObject>()) {
          GenericTriggerBuilder genericTrigger = new GenericTriggerBuilder(this.contractTypeBuilder, skipIfTriggerObject, "DelaySkipIfTrigger");
          ActivateDelayTriggerResult triggerResult = ScriptableObject.CreateInstance<ActivateDelayTriggerResult>();
          triggerResult.DelayResult = result;
          triggerResult.Type = "SkipIf";
          genericTrigger.Results = new List<DesignResult>() { triggerResult };

          result.AddSkipIfTrigger(skipIfType, genericTrigger.BuildTrigger());
        }
      }

      // Build Complete Early triggers
      if (resultObject.ContainsKey("CompleteEarlyTriggers")) {
        JArray completeEarlyTriggersArray = (JArray)resultObject["CompleteEarlyTriggers"];
        foreach (JObject completeEarlyTriggerObject in completeEarlyTriggersArray.Children<JObject>()) {
          GenericTriggerBuilder genericTrigger = new GenericTriggerBuilder(this.contractTypeBuilder, completeEarlyTriggerObject, "DelayCompleteEarlyTrigger");
          ActivateDelayTriggerResult triggerResult = ScriptableObject.CreateInstance<ActivateDelayTriggerResult>();
          triggerResult.DelayResult = result;
          triggerResult.Type = "CompleteEarly";
          genericTrigger.Results = new List<DesignResult>() { triggerResult };

          result.CompleteEarlyTriggers.Add(genericTrigger.BuildTrigger());
        }
      }

      // Build Cancel triggers
      if (resultObject.ContainsKey("CancelTriggers")) {
        JArray cancelTriggersArray = (JArray)resultObject["CancelTriggers"];
        foreach (JObject cancelTriggerObject in cancelTriggersArray.Children<JObject>()) {
          GenericTriggerBuilder genericTrigger = new GenericTriggerBuilder(this.contractTypeBuilder, cancelTriggerObject, "DelayCancelTrigger");
          ActivateDelayTriggerResult triggerResult = ScriptableObject.CreateInstance<ActivateDelayTriggerResult>();
          triggerResult.DelayResult = result;
          triggerResult.Type = "Cancel";
          genericTrigger.Results = new List<DesignResult>() { triggerResult };

          result.CancelTriggers.Add(genericTrigger.BuildTrigger());
        }
      }

      results.Add(result);
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

    private void BuildDebugStatLoggerResult(JObject resultObject) {
      Main.LogDebug("[BuildDebugStatLoggerResult] Building 'BuildDebugStatLogger' result");
      string scopeRaw = resultObject["Scope"].ToString();
      Scope scope = (Scope)Enum.Parse(typeof(Scope), scopeRaw);

      string key = resultObject.ContainsKey("Key") ? resultObject["Key"].ToString() : "All";

      DebugStatLoggerResult result = ScriptableObject.CreateInstance<DebugStatLoggerResult>();
      result.Scope = scope;
      result.Key = key;

      results.Add(result);
    }

    private void BuildDebugTagLoggerResult(JObject resultObject) {
      Main.LogDebug("[BuildDebugTagLoggerResult] Building 'BuildDebugTagLogger' result");
      string scopeRaw = resultObject["Scope"].ToString();
      Scope scope = (Scope)Enum.Parse(typeof(Scope), scopeRaw);

      string key = resultObject.ContainsKey("Key") ? resultObject["Key"].ToString() : "All";

      DebugTagLoggerResult result = ScriptableObject.CreateInstance<DebugTagLoggerResult>();
      result.Scope = scope;
      result.Key = key;

      results.Add(result);
    }

    private void BuildSetStatResult(JObject resultObject) {
      Main.LogDebug("[BuildSetStatResult] Building 'BuildSetStat' result");
      string scopeRaw = resultObject["Scope"].ToString();
      Scope scope = (Scope)Enum.Parse(typeof(Scope), scopeRaw);

      string key = resultObject["Key"].ToString();

      string dataTypeRaw = resultObject["DataType"].ToString();
      DataType dataType = (DataType)Enum.Parse(typeof(DataType), dataTypeRaw);

      string operationRaw = resultObject["Operation"].ToString();
      StatOperation operation = (StatOperation)Enum.Parse(typeof(StatOperation), operationRaw);

      string value = resultObject.ContainsKey("Value") ? resultObject["Value"].ToString() : "";

      SetStatResult result = ScriptableObject.CreateInstance<SetStatResult>();
      result.Scope = scope;
      result.Key = key;
      result.DataType = dataType;
      result.Operation = operation;
      result.Value = value;

      results.Add(result);
    }

    private void BuildSetTagResult(JObject resultObject) {
      Main.LogDebug("[BuildSetTagResult] Building 'BuildSetTag' result");
      string scopeRaw = resultObject["Scope"].ToString();
      Scope scope = (Scope)Enum.Parse(typeof(Scope), scopeRaw);

      string operationRaw = resultObject["Operation"].ToString();
      TagOperation operation = (TagOperation)Enum.Parse(typeof(TagOperation), operationRaw);

      string tag = resultObject["Tag"].ToString();

      SetTagResult result = ScriptableObject.CreateInstance<SetTagResult>();
      result.Scope = scope;
      result.Operation = operation;
      result.Tag = tag;

      results.Add(result);
    }

    private void BuildSetAIPatrolRouteResult(JObject resultObject) {
      Main.LogDebug("[BuildSetAIPatrolRoute] Building 'BuildSetAIPatrolRoute' result");
      string unitGroupTypeRaw = resultObject["UnitGroupType"].ToString();
      UnitGroupType unitGroupType = (UnitGroupType)Enum.Parse(typeof(UnitGroupType), unitGroupTypeRaw);

      string unitTypeGUID = resultObject["UnitTypeGuid"].ToString();
      string routeGUID = resultObject["RouteGuid"].ToString();
      bool followForward = (bool)resultObject["FollowForward"];
      bool shouldSprint = (bool)resultObject["ShouldSprint"];
      bool startAtClosestPoint = (bool)resultObject["StartAtClosestPoint"];

      SetAIPatrolRouteResult result = ScriptableObject.CreateInstance<SetAIPatrolRouteResult>();
      result.UnitGroupType = unitGroupType;
      result.UnitTypeGUID = unitTypeGUID;
      result.RouteGUID = routeGUID;
      result.FollowForward = followForward;
      result.ShouldSprint = shouldSprint;
      result.StartAtClosestPoint = startAtClosestPoint;

      results.Add(result);
    }

    private void BuildSetAIBehaviourTreeResult(JObject resultObject) {
      Main.LogDebug("[BuildSetAIBehaviourTree] Building 'BuildSetAIBehaviourTree' result");
      string unitGroupTypeRaw = resultObject["UnitGroupType"].ToString();
      UnitGroupType unitGroupType = (UnitGroupType)Enum.Parse(typeof(UnitGroupType), unitGroupTypeRaw);

      string unitTypeGUID = resultObject["UnitTypeGuid"].ToString();
      string behaviourTreeRaw = resultObject["BehaviourTree"].ToString();
      BehaviorTreeIDEnum behaviourTree = (BehaviorTreeIDEnum)Enum.Parse(typeof(BehaviorTreeIDEnum), behaviourTreeRaw);

      SetAIBehaviourTreeResult result = ScriptableObject.CreateInstance<SetAIBehaviourTreeResult>();
      result.UnitGroupType = unitGroupType;
      result.UnitTypeGUID = unitTypeGUID;
      result.BehaviourTree = behaviourTree;

      results.Add(result);
    }

    private void BuildSwapTeamsResult(JObject resultObject) {
      Main.LogDebug("[BuildSwapTeams] Building 'BuildSwapTeams' result");
      string team1GUID = resultObject["Team1Guid"].ToString();
      string team2GUID = resultObject["Team2Guid"].ToString();

      SwapTeamsResult result = ScriptableObject.CreateInstance<SwapTeamsResult>();
      result.Team1GUID = team1GUID;
      result.Team2GUID = team2GUID;

      results.Add(result);
    }

    private void BuildSetAllTeamsRelationshipResult(JObject resultObject) {
      Main.LogDebug("[BuildSetAllTeamsFriendly] Building 'BuildSetAllTeamsRelationship' result");
      bool enabled = (bool)resultObject["Enabled"];
      string relationship = resultObject["Relationship"].ToString();

      SetAllTeamsRelationshipResult result = ScriptableObject.CreateInstance<SetAllTeamsRelationshipResult>();
      result.Enabled = enabled;
      result.Relationship = relationship;

      results.Add(result);
    }
  }
}