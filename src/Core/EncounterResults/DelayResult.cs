using UnityEngine;

using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Trigger;
using MissionControl.Logic;

namespace MissionControl.Result {
  public class DelayResult : EncounterResult {
    public string Name { get; set; } = "Unnamed Delay Result";
    public float Time { get; set; } = -1;
    public int Rounds { get; set; } = -1;
    public int Phases { get; set; } = -1;
    public List<DesignResult> Results { get; set; }
    public List<DesignResult> ResultsIfSkipped { get; set; }

    public string SkipIfExecution { get; set; }

    private int roundCount = 0;
    private int phaseCount = 0;

    private bool activated = false;
    private bool completed = false;

    private bool waitingUntilAfterDelayToSkipIf = false;
    private bool useSkippedState = false;
    private bool useCompleteEarlyState = false;
    private bool useCancelState = false;

    private Dictionary<string, List<GenericTrigger>> SkipIfTriggers { get; set; } = new Dictionary<string, List<GenericTrigger>>();
    public List<GenericTrigger> CompleteEarlyTriggers = new List<GenericTrigger>();
    public List<GenericTrigger> CancelTriggers = new List<GenericTrigger>();

    private void InitResults() {
      foreach (DesignResult result in Results) {
        result.AlwaysInit(UnityGameInstance.Instance.Game.Combat);
      }

      foreach (DesignResult result in ResultsIfSkipped) {
        result.AlwaysInit(UnityGameInstance.Instance.Game.Combat);
      }
    }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[DelayResult] Delaying '{Name}' results for '{Time}' seconds, '{Rounds}' rounds and/or '{Phases}' phases");
      activated = true;
      SubscribeToMessages(true);

      InitResults();

      if (useCancelState) {
        Cancel();
        return;
      }

      if (useCompleteEarlyState) {
        // Ignore any delay and run the normal logic
        TriggerResults();
        return;
      }

      if (useSkippedState) {
        // Ignore any delay and run the SkipIf logic
        TriggerResultsIfSkipped();
        return;
      }

      Main.LogDebug($"[DelayResult.TriggerResults] About to build SkipIf triggers for 'WatchDuringDelay'");
      BuildSkipIfTriggersForType("WatchDuringDelay");

      Main.LogDebug($"[DelayResult.TriggerResults] About to build Other triggers for 'WatchDuringDelay'");
      BuildOtherTriggers();

      if (IsTimeControlled()) {
        MissionControl.Instance.EncounterLayerParent.StartCoroutine(DelayResultsWithTime());
      }
    }

    private void BuildOtherTriggers() {
      CompleteEarlyTriggers.ForEach(trigger => trigger.BuildAndRunImmediately());
      CancelTriggers.ForEach(trigger => trigger.BuildAndRunImmediately());
    }

    private void BuildSkipIfTriggersForType(string type) {
      if (SkipIfTriggers.ContainsKey(type)) {
        foreach (GenericTrigger trigger in SkipIfTriggers[type]) {
          Main.LogDebug($"[DelayResult.Trigger] Attaching trigger DelayResult '{Name}' since type is '{type}'");
          if (type == "BuildAndRunImmediately") {
            trigger.BuildAndRunImmediately();
          } else {
            trigger.Run(new GenericTriggerPayload(shouldManuallyInitialise: true));
          }
        }
      }
    }

    public void SubscribeToMessages(bool subscribe) {
      if (IsRoundControlled()) UnityGameInstance.BattleTechGame.MessageCenter.Subscribe(MessageCenterMessageType.OnRoundBegin, new ReceiveMessageCenterMessage(this.OnRoundBegin), subscribe);
      if (IsPhaseControlled()) UnityGameInstance.BattleTechGame.MessageCenter.Subscribe(MessageCenterMessageType.OnPhaseBegin, new ReceiveMessageCenterMessage(this.OnPhaseBegin), subscribe);
    }

    public void AddSkipIfTrigger(string type, GenericTrigger genericTrigger) {
      if (!SkipIfTriggers.ContainsKey(type)) SkipIfTriggers.Add(type, new List<GenericTrigger>());
      SkipIfTriggers[type].Add(genericTrigger);

      if (type == "WatchFromContractStart") {
        Main.LogDebug($"[DelayResult.AddSkipIfTrigger] About to run SkipIf trigger for 'WatchFromContractStart'");
        genericTrigger.Run(null);
      }
    }

    IEnumerator DelayResultsWithTime() {
      yield return new WaitForSeconds(Time);
      Main.LogDebug("[DelayResult] Triggering '{Name}' with DelayResultsWithTime");
      TriggerResults();
    }

    public void UseSkippedState() {
      useSkippedState = true;

      if (activated && !completed) {
        TriggerResultsIfSkipped();
      }
    }

    public void UseCompleteEarlyState() {
      useCompleteEarlyState = true;

      if (activated && !completed) {
        TriggerResults();
      }
    }

    public void UseCancelState() {
      useCancelState = true;

      if (activated && !completed) {
        Cancel();
      }
    }

    private bool IsTimeControlled() {
      return Time > 0 ? true : false;
    }

    private bool IsRoundControlled() {
      return Rounds > 0 ? true : false;
    }

    private bool IsPhaseControlled() {
      return Phases > 0 ? true : false;
    }

    private void OnRoundBegin(MessageCenterMessage message) {
      if (completed || !IsRoundControlled()) return;
      roundCount++;

      if (roundCount >= Rounds) {
        Main.LogDebug($"[DelayResult] Triggering '{Name}' with OnRoundBegin");
        TriggerResults();
      }
    }

    private void OnPhaseBegin(MessageCenterMessage message) {
      if (completed || !IsPhaseControlled()) return;
      phaseCount++;

      if (phaseCount >= Phases) {
        Main.LogDebug($"[DelayResult] Triggering '{Name}' with OnPhaseBegin");
        TriggerResults();
      }
    }

    private void TriggerResults() {
      Main.LogDebug($"[DelayResult.TriggerResults] About to build SkipIf triggers and run immediately for 'CheckAtEndOfDelay'");
      BuildSkipIfTriggersForType("CheckAtEndOfDelay");

      Main.LogDebug($"[DelayResult.TriggerResults] useSkippedState '{useSkippedState}'");

      if (waitingUntilAfterDelayToSkipIf) {
        // Final execution is to use the SkipIf results and not the normal results
        TriggerResultsIfSkipped();
        return;
      }

      if (completed) return;
      completed = true;
      SubscribeToMessages(false);

      if (Results != null) {
        Main.LogDebug($"[DelayResult.TriggerResults] Triggering '{Name}' delayed results");
        foreach (DesignResult result in Results) {
          result.Trigger(null, null);
        }
      } else {
        Main.Logger.LogError($"[DelayResult.TriggerResults] Results list is null. This is a serious issue with the contract builder. Check your custom contract type build file for errors.");
      }

      DeleteTriggers();
    }

    private void TriggerResultsIfSkipped() {
      if (SkipIfExecution == "AfterDelay" && waitingUntilAfterDelayToSkipIf == false) {
        waitingUntilAfterDelayToSkipIf = true;
        return;
      }

      if (completed) return;
      completed = true;
      SubscribeToMessages(false);

      if (ResultsIfSkipped != null) {
        Main.LogDebug($"[DelayResult.TriggerResultsIfSkipped] Triggering '{Name}' skip results");
        foreach (DesignResult result in ResultsIfSkipped) {
          result.Trigger(null, null);
        }
      } else {
        Main.Logger.LogDebug($"[DelayResult.TriggerResultsIfSkipped] Skip Results list is null. This means you've set up a 'SkipIf' trigger but with no results. This can be expected.");
      }

      DeleteTriggers();
    }

    private void DeleteTriggers() {
      SkipIfTriggers.Values.ToList().ForEach(triggerList => triggerList.ForEach(trigger => trigger.Delete()));
      SkipIfTriggers.Clear();

      CompleteEarlyTriggers.ForEach(trigger => trigger.Delete());
      CompleteEarlyTriggers.Clear();

      CancelTriggers.ForEach(trigger => trigger.Delete());
      CancelTriggers.Clear();
    }

    public void Cancel() {
      Main.LogDebug($"[DelayResult.Cancel] Cancelling DelayResult '{Name}'");
      completed = true;
      SubscribeToMessages(false);
      DeleteTriggers();
    }
  }
}
