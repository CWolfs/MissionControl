using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Trigger;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class GenericTriggerBuilder : TriggerBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject trigger;

    private string name;
    private string triggerOn;
    private MessageCenterMessageType triggerMessageType;
    private string description;

    private DesignConditional conditional;
    private List<DesignResult> results;

    public GenericTriggerBuilder(ContractTypeBuilder contractTypeBuilder, JObject trigger, string name) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.trigger = trigger;

      this.name = name;
      this.triggerOn = trigger["TriggerOn"].ToString();
      this.description = trigger["Description"].ToString();

      if (trigger.ContainsKey("Conditional")) {
        ConditionalBuilder resultsBuilder = new ConditionalBuilder(contractTypeBuilder, (JObject)trigger["Conditional"]);
        conditional = resultsBuilder.Build();
      }

      if (trigger.ContainsKey("Results")) {
        ResultsBuilder resultsBuilder = new ResultsBuilder(contractTypeBuilder, (JArray)trigger["Results"]);
        results = resultsBuilder.Build();
      } else {
        Main.Logger.LogError("[GenericTriggerBuilder] Generic Triggers require 'Results'");
      }

      triggerMessageType = (MessageCenterMessageType)Enum.Parse(typeof(MessageCenterMessageType), this.triggerOn);
    }

    public override void Build() {
      Main.LogDebug("[GenericTriggerBuilder] Building 'Generic' trigger");
      GenericTrigger genericTrigger = new GenericTrigger(this.name, this.description, this.triggerMessageType, null, results);
      genericTrigger.Run(null);
    }
  }
}