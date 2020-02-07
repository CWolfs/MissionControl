using UnityEngine;

using Newtonsoft.Json.Linq;

using BattleTech.Designed;
using BattleTech.Framework;

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
        default:
          Main.Logger.LogError($"[ResultsBuilder.{contractTypeBuilder.ContractTypeKey}] No valid result was built for '{type}'");
          break;
      }
    }

    private void BuildExecuteGameLogicResult(JObject resultObject) {
      Main.LogDebug("[BuildExecuteGameLogicResult] Building ExecuteGameLogic result");
      string chunkGuid = (resultObject.ContainsKey("ChunkGuid")) ? resultObject["ChunkGuid"].ToString() : null;
      string encounterGuid = (resultObject.ContainsKey("EncounterGuid")) ? resultObject["EncounterGuid"].ToString() : null;

      ExecuteGameLogicResult result = ScriptableObject.CreateInstance<ExecuteGameLogicResult>();
      result.ChunkGuid = chunkGuid;
      result.EncounterGuid = encounterGuid;

      results.Add(result);
    }

    private void BuildDialogueGameLogicResult(JObject resultObject) {
      Main.LogDebug("[BuildDialogueGameLogicResult] Building Dialogue result");
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
  }
}