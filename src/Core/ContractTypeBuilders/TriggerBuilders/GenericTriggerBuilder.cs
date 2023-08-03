using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;
using BattleTech.Designed;

using MissionControl.Trigger;
using MissionControl.Messages;
using MissionControl.Conditional;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class GenericTriggerBuilder : TriggerBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject trigger;

    private string name;
    private string triggerOn;
    private MessageCenterMessageType triggerMessageType;
    private string description;

    private string conditionalEvaluationString;
    private LogicEvaluation conditionalEvaluation;
    private GenericCompoundConditional conditional;
    public List<DesignResult> Results { get; set; }

    public GenericTriggerBuilder(ContractTypeBuilder contractTypeBuilder, JObject trigger, string name) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.trigger = trigger;

      this.name = name;
      this.triggerOn = trigger["TriggerOn"].ToString();
      this.description = (trigger.ContainsKey("Description")) ? trigger["Description"].ToString() : "";

      this.conditionalEvaluationString = (trigger.ContainsKey("SucceedOn")) ? trigger["SucceedOn"].ToString() : "All";
      this.conditionalEvaluation = (LogicEvaluation)Enum.Parse(typeof(LogicEvaluation), conditionalEvaluationString);

      if (trigger.ContainsKey("Conditionals")) {
        ConditionalBuilder conditionalBuilder = new ConditionalBuilder(contractTypeBuilder, (JArray)trigger["Conditionals"]);
        this.conditional = conditionalBuilder.Build();
        this.conditional.whichMustBeTrue = this.conditionalEvaluation;
      }

      if (trigger.ContainsKey("Results")) {
        ResultsBuilder resultsBuilder = new ResultsBuilder(contractTypeBuilder, (JArray)trigger["Results"]);
        this.Results = resultsBuilder.Build();
      }

      if (!Enum.TryParse(this.triggerOn, out triggerMessageType)) {
        MessageTypes messageType;
        if (!Enum.TryParse(this.triggerOn, out messageType)) {
          Main.Logger.LogError("[GenericTriggerBuilder] Invalid 'TriggerOn' provided.");
        } else {
          triggerMessageType = (MessageCenterMessageType)messageType;
        }
      }
    }

    public GenericTriggerBuilder(ContractTypeBuilder contractTypeBuilder, string name, MessageCenterMessageType triggerMessageType, GenericCompoundConditional conditional, string description, List<DesignResult> results) {
      this.name = name;
      this.triggerMessageType = triggerMessageType;
      this.conditional = conditional;
      this.description = description;
      this.Results = results;
    }

    public override void Build() {
      Main.LogDebug("[GenericTriggerBuilder] Building 'Generic' trigger");

      if (this.Results == null) {
        Main.Logger.LogError("[GenericTriggerBuilder] Generic Triggers require 'Results'");
      } else {
        GenericTrigger genericTrigger = new GenericTrigger(this.name, this.description, this.triggerMessageType, this.conditional, Results);
        genericTrigger.Run(null);
      }
    }

    public GenericTrigger BuildTrigger() {
      Main.LogDebug("[GenericTriggerBuilder] Building 'Generic' trigger");

      if (this.Results == null) {
        Main.Logger.LogError("[GenericTriggerBuilder] Generic Triggers require 'Results'");
      } else {
        return new GenericTrigger(this.name, this.description, this.triggerMessageType, this.conditional, Results);
      }

      return null;
    }
  }
}