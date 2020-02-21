using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Trigger;
using MissionControl.Messages;

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
    private List<DesignResult> results;

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
        this.results = resultsBuilder.Build();
      } else {
        Main.Logger.LogError("[GenericTriggerBuilder] Generic Triggers require 'Results'");
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

    public override void Build() {
      Main.LogDebug("[GenericTriggerBuilder] Building 'Generic' trigger");
      GenericTrigger genericTrigger = new GenericTrigger(this.name, this.description, this.triggerMessageType, this.conditional, results);
      genericTrigger.Run(null);
    }
  }
}