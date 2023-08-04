using UnityEngine;

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

    private string Type { get; set; }
    private GenericTrigger SkipIfTrigger { get; set; }

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

      if (useSkippedState) {
        // Ignore any delay and run the SkipIf logic
        TriggerResultsIfSkipped();
        return;
      }

      if (Type == "WatchDuringDelay") {
        Main.LogDebug($"[DelayResult.Trigger] Attaching trigger for SkipIf since 'WatchDuringDelay' is set");
        SkipIfTrigger.Run(new GenericTriggerPayload(shouldManuallyInitialise: true));
      }

      if (IsTimeControlled()) {
        MissionControl.Instance.EncounterLayerParent.StartCoroutine(DelayResultsWithTime());
      }
    }

    public void SubscribeToMessages(bool subscribe) {
      if (IsRoundControlled()) UnityGameInstance.BattleTechGame.MessageCenter.Subscribe(MessageCenterMessageType.OnRoundBegin, new ReceiveMessageCenterMessage(this.OnRoundBegin), subscribe);
      if (IsPhaseControlled()) UnityGameInstance.BattleTechGame.MessageCenter.Subscribe(MessageCenterMessageType.OnPhaseBegin, new ReceiveMessageCenterMessage(this.OnPhaseBegin), subscribe);
    }

    public void SetTrigger(string type, GenericTrigger genericTrigger) {
      Type = type;
      SkipIfTrigger = genericTrigger;

      if (Type == "WatchFromContractStart") {
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
      if (Type == "CheckAtEndOfDelay") {
        Main.LogDebug($"[DelayResult.TriggerResults] Checking SkipIf trigger since 'CheckAtEndOfDelay' is set");
        SkipIfTrigger.BuildAndRunImmediately();
      }

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

      // Delete trigger
      if (SkipIfTrigger != null) SkipIfTrigger.Delete();
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

      // Delete trigger
      if (SkipIfTrigger != null) SkipIfTrigger.Delete();
    }
  }
}
