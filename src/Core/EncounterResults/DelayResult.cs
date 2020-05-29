using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Result {
  public class DelayResult : EncounterResult {
    public string Name { get; set; } = "Unnamed Delay Result";
    public float Time { get; set; } = -1;
    public int Rounds { get; set; } = -1;
    public int Phases { get; set; } = -1;
    public List<DesignConditional> SkipIf { get; set; }
    public List<DesignResult> Results { get; set; }
    public List<DesignResult> ResultsIfSkipped { get; set; }
    private int roundCount = 0;
    private int phaseCount = 0;
    private bool completed = false;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[DelayResult] Delaying '{Name}' results for '{Time}' seconds, '{Rounds}' rounds and/or '{Phases}' phases");

      SubscribeToMessages(true);
      if (IsTimeControlled()) MissionControl.Instance.EncounterLayerParent.StartCoroutine(DelayResultsWithTime());

      if (SkipIf != null) {
        SubscribeToSkipConditions(true);
      }
    }

    public void SubscribeToMessages(bool subscribe) {
      if (IsRoundControlled()) UnityGameInstance.BattleTechGame.MessageCenter.Subscribe(MessageCenterMessageType.OnRoundBegin, new ReceiveMessageCenterMessage(this.OnRoundBegin), subscribe);
      if (IsPhaseControlled()) UnityGameInstance.BattleTechGame.MessageCenter.Subscribe(MessageCenterMessageType.OnPhaseBegin, new ReceiveMessageCenterMessage(this.OnPhaseBegin), subscribe);
    }

    private void SubscribeToSkipConditions(bool subscribe) {
      foreach (DesignConditional conditional in SkipIf) {

      }
    }

    IEnumerator DelayResultsWithTime() {
      yield return new WaitForSeconds(Time);
      Main.LogDebug("[DelayResult] Triggering '{Name}' with DelayResultsWithTime");
      TriggerResults();
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
      if (completed) return;
      completed = true;
      SubscribeToMessages(false);

      if (Results != null) {
        Main.LogDebug($"[DelayResult] Triggering '{Name}' delayed results");
        foreach (DesignResult result in Results) {
          result.Trigger(null, null);
        }
      } else {
        Main.Logger.LogError($"[DelayResult] Results list is null. This is a serious issue with the contract builder. Check your custom contract type build file for errors.");
      }
    }

    public void TriggerResultsIfSkipped() {
      if (completed) return;
      completed = true;
      SubscribeToMessages(false);
    }
  }
}
