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

      MessageCenterMessageType messageType;
      if (trigger != null) {
        if (!Enum.TryParse(trigger, out messageType)) {
          MessageTypes customMessageType;
          if (!Enum.TryParse(trigger, out customMessageType)) {
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

      // Parse decision options from the JSON
      if (!dialogue.ContainsKey("Options")) {
        Main.Logger.LogError($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] Decision dialogue '{this.name}' is missing 'Options' array");
        return;
      }

      JArray optionsArray = (JArray)dialogue["Options"];
      List<DialogueDecisionOption> options = new List<DialogueDecisionOption>();

      foreach (JToken optionToken in optionsArray) {
        JObject option = (JObject)optionToken;
        if (!option.ContainsKey("ButtonText")) {
          Main.Logger.LogError($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] Decision option missing 'ButtonText'");
          continue;
        }

        string buttonText = option["ButtonText"].ToString();
        List<DesignResult> results = new List<DesignResult>();

        // Parse results for this option if they exist
        if (option.ContainsKey("Results")) {
          ResultsBuilder resultsBuilder = new ResultsBuilder(contractTypeBuilder, (JArray)option["Results"]);
          results = resultsBuilder.Build();
        }

        bool closeOnClick = option.ContainsKey("CloseOnClick") ? (bool)option["CloseOnClick"] : true;

        // Parse optional conditional for show/hide logic
        DesignConditional conditional = null;
        if (option.ContainsKey("Conditional")) {
          JArray conditionalArray = new JArray();
          conditionalArray.Add(option["Conditional"]);
          ConditionalBuilder conditionalBuilder = new ConditionalBuilder(contractTypeBuilder, conditionalArray);
          List<DesignConditional> conditionals = conditionalBuilder.BuildAsDesignConditionals();
          if (conditionals.Count > 0) {
            conditional = conditionals[0];
          }
        }

        // Parse optional branching fields
        string nextDialogueGuid = option.ContainsKey("NextDialogueGuid") ? option["NextDialogueGuid"].ToString() : null;
        int nextContentIndex = option.ContainsKey("NextContentIndex") ? (int)option["NextContentIndex"] : -1;

        // Create decision option
        DialogueDecisionOption decisionOption = new DialogueDecisionOption(buttonText, results, closeOnClick);
        decisionOption.Conditional = conditional;
        decisionOption.NextDialogueGuid = nextDialogueGuid;
        decisionOption.NextContentIndex = nextContentIndex;

        options.Add(decisionOption);

        Main.Logger.Log($"[DialogueBuilder.{contractTypeBuilder.ContractTypeKey}] Added decision option: {buttonText} with {results.Count} results");
      }

      // Build encounter objects dictionary for ApplyContractOverride
      Dictionary<string, EncounterObjectGameLogic> encounterObjects = new Dictionary<string, EncounterObjectGameLogic>();
      MissionControl.Instance.EncounterLayerData.BuildEncounterObjectDictionary(encounterObjects);

      // Create the DialogueDecisionGameLogic
      // Note: dialogueOverride is null here - it will be applied later by the contract override system
      DialogueFactory.CreateDialogueDecisionLogic(parent, this.name, this.guid, null, options, encounterObjects);
    }
  }
}