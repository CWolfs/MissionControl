using UnityEngine;

using System.Linq;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Result;
using MissionControl.Conditional;
using MissionControl.Logic;

namespace MissionControl.Trigger {
  public class GenericTrigger : EncounterTrigger {
    private string name;
    private string description;
    private MessageCenterMessageType onMessage;
    private DesignConditional conditional;
    private List<DesignResultBox> results;

    public GenericTrigger(string name, string description, MessageCenterMessageType onMessage, DesignConditional conditional, List<DesignResult> results) {
      this.name = name;
      this.description = description;
      this.onMessage = onMessage;
      this.conditional = conditional;
      this.results = results.Select(r => (DesignResultBox)new EncounterResultBox(r)).ToList();

      if (this.conditional == null) {
        this.conditional = ScriptableObject.CreateInstance<AlwaysTrueConditional>();
      }
    }

    public override void Run(RunPayload payload) {
      Main.LogDebug($"[GenericTrigger] Setting up trigger '{this.name}'");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse trigger = new SmartTriggerResponse();
      trigger.inputMessage = onMessage;
      trigger.designName = $"{description} on {onMessage}";
      trigger.conditionalbox = new EncounterConditionalBox(conditional);

      trigger.resultList.contentsBox = this.results;
      encounterData.responseGroup.triggerList.Add(trigger);
    }
  }
}