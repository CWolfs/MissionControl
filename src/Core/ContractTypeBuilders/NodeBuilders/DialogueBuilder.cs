using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Trigger;
using MissionControl.EncounterFactories;
using MissionControl.Messages;
using MissionControl.Logic;

using Newtonsoft.Json.Linq;
using BattleTech.Framework;

namespace MissionControl.ContractTypeBuilders {
  public class DialogueBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject dialogue;

    private GameObject parent;
    private string name;
    private string subType;
    private string guid;

    public DialogueBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject dialogue) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.dialogue = dialogue;

      this.parent = parent;
      this.name = dialogue["Name"].ToString();
      this.subType = dialogue["SubType"].ToString();
      this.guid = dialogue["Guid"].ToString();
    }

    public override void Build() {
      switch (subType) {
        case "Simple": BuildSimpleDialogue(parent, dialogue); break;
        case "Sequence": BuildSequenceDialogue(parent, dialogue); break;
        case "Decision": BuildDecisionDialogue(parent, dialogue); break;
        default: Main.LogDebug($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildSimpleDialogue(GameObject parent, JObject dialogue) {
      string trigger = dialogue.ContainsKey("Trigger") ? dialogue["Trigger"].ToString() : null;
      bool showOnlyOnce = dialogue.ContainsKey("ShowOnlyOnce") ? (bool)dialogue["ShowOnlyOnce"] : false;

      DialogueFactory.CreateDialogLogic(parent, this.name, this.guid, showOnlyOnce);

      if (trigger != null) {
        if (!Enum.TryParse(trigger, out MessageCenterMessageType messageType)) {
          if (!Enum.TryParse(trigger, out MessageTypes customMessageType)) {
            Main.Logger.LogError("[DialogueBuilder] Invalid 'Trigger' provided.");
          } else {
            messageType = (MessageCenterMessageType)customMessageType;
          }
        }

        DialogTrigger dialogueTrigger = new DialogTrigger(messageType, this.guid);
        dialogueTrigger.Run();
      }
    }

    private void BuildSequenceDialogue(GameObject parent, JObject dialogue) {
      bool isInterrupt = dialogue.ContainsKey("IsInterrupt") ? (bool)dialogue["IsInterrupt"] : true;
      List<string> dialogueGUIDs = dialogue.ContainsKey("DialogueGuids") ? dialogue["DialogueGuids"].ToObject<List<string>>() : null;

      DialogueFactory.CreateDialogueSequenceLogic(parent, this.name, this.guid, isInterrupt, dialogueGUIDs);
    }

    private void BuildDecisionDialogue(GameObject parent, JObject dialogue) {
      Main.Logger.Log($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] Building Decision dialogue: {this.name}");

      if (!dialogue.ContainsKey("Options")) {
        Main.Logger.LogError($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] Decision dialogue '{this.name}' is missing 'Options' array");
        return;
      }

      JArray optionsArray = (JArray)dialogue["Options"];
      List<DialogueDecisionOption> options = new List<DialogueDecisionOption>();

      foreach (JToken optionToken in optionsArray) {
        JObject option = (JObject)optionToken;
        if (!option.ContainsKey("Text")) {
          Main.Logger.LogError($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] Decision option missing 'Text'");
          continue;
        }

        string responseText = option["Text"].ToString();
        List<DesignResult> results = new List<DesignResult>();

        if (option.ContainsKey("Results")) {
          ResultsBuilder resultsBuilder = new ResultsBuilder(contractTypeBuilder, (JArray)option["Results"]);
          results = resultsBuilder.Build();
        }

        GenericCompoundConditional conditional = null;

        // Parse SucceedOn (LogicEvaluation) - default to "All"
        string succeedOnString = option.ContainsKey("SucceedOn") ? option["SucceedOn"].ToString() : "All";
        LogicEvaluation logicEvaluation = (LogicEvaluation)Enum.Parse(typeof(LogicEvaluation), succeedOnString);

        // Parse conditionals array
        if (option.ContainsKey("Conditionals")) {
          JArray conditionalArray = (JArray)option["Conditionals"];
          if (conditionalArray.Count > 0) {
            ConditionalBuilder conditionalBuilder = new ConditionalBuilder(contractTypeBuilder, conditionalArray);
            conditional = conditionalBuilder.Build();
            conditional.whichMustBeTrue = logicEvaluation;
          }
        }

        string nextDialogueGuid = option.ContainsKey("NextDialogueGuid") ? option["NextDialogueGuid"].ToString() : null;
        int nextContentIndex = option.ContainsKey("NextContentIndex") ? (int)option["NextContentIndex"] : -1;

        DialogueDecisionOption decisionOption = new DialogueDecisionOption(responseText, results);
        decisionOption.Conditional = conditional;
        decisionOption.NextDialogueGuid = nextDialogueGuid;
        decisionOption.NextContentIndex = nextContentIndex;

        options.Add(decisionOption);

        Main.Logger.Log($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] Added decision option: {responseText} with {results.Count} results");
      }

      Dictionary<string, EncounterObjectGameLogic> encounterObjects = new Dictionary<string, EncounterObjectGameLogic>();
      MissionControl.Instance.EncounterLayerData.BuildEncounterObjectDictionary(encounterObjects);

      // Note: dialogueOverride is null here - it will be applied later by the contract override system
      DialogueFactory.CreateDialogueDecisionLogic(parent, this.name, this.guid, null, options, encounterObjects);
    }
  }
}