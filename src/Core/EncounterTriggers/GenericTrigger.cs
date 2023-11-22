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

    // TODO: Support 'SmartTriggerResponse.onlyTriggeredOnce' false to allow for triggers to run multiple times
    // TODO: I don't think the Delay trigger for SkipIf should support it though (maybe?)

    private SmartTriggerResponse trigger;

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
      GenericTriggerPayload genericTriggerPayload = payload as GenericTriggerPayload;

      Main.LogDebug($"[GenericTrigger.Run] Setting up trigger '{this.name}'");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      trigger = new SmartTriggerResponse();
      trigger.inputMessage = onMessage;
      trigger.designName = $"{(!string.IsNullOrEmpty(this.name) ? this.name : description)} on {onMessage}";
      trigger.conditionalbox = new EncounterConditionalBox(conditional);

      trigger.resultList.contentsBox = this.results;
      encounterData.responseGroup.triggerList.Add(trigger);

      if (genericTriggerPayload != null) {
        if (genericTriggerPayload.ShouldManuallyInitialise) {
          Main.LogDebug($"[GenericTrigger.Run] Manually initialising and listening for messages on trigger '{this.name}'");
          trigger.AlwaysInit(MissionControl.Instance.EncounterLayerData.Combat);
          trigger.ContractInitialize(MissionControl.Instance.EncounterLayerData);
          trigger.ListenToMessages(MissionControl.Instance.EncounterLayerData.Combat.MessageCenter);
        }
      }
    }

    public SmartTriggerResponse BuildAndRunImmediately() {
      Main.LogDebug($"[GenericTrigger.BuildAndRunImmediately] Setting up trigger '{this.name}'");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      trigger = new SmartTriggerResponse();
      trigger.inputMessage = onMessage;
      trigger.designName = $"{(!string.IsNullOrEmpty(this.name) ? this.name : description)} on {onMessage}";
      trigger.conditionalbox = new EncounterConditionalBox(conditional);

      trigger.resultList.contentsBox = this.results;

      Main.LogDebug($"[GenericTrigger.BuildAndRunImmediately] Manually initialising and listening for messages on trigger '{this.name}'");
      trigger.ContractInitialize(MissionControl.Instance.EncounterLayerData);

      trigger.OnMessage(null);

      return trigger;
    }

    public void Delete() {
      Main.LogDebug($"[GenericTrigger] Deleting trigger '{this.name}'");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      encounterData.responseGroup.triggerList.Remove(trigger);
    }
  }
}