using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

namespace MissionControl.Result {
  public class DelayResult : EncounterResult {
    public float Time { get; set; }
    public List<DesignResult> Results { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[DelayResult] Delaying results for '{Time}' seconds");
      MissionControl.Instance.EncounterLayerParent.StartCoroutine(DelayResults());
    }

    IEnumerator DelayResults() {
      yield return new WaitForSeconds(Time);
      TriggerResults();
    }

    private void TriggerResults() {
      if (Results != null) {
        Main.LogDebug("[DelayResult] Triggering delayed results");
        foreach (DesignResult result in Results) {
          result.Trigger(null, null);
        }
      } else {
        Main.Logger.LogError("[DelayResult] Results list is null. This is a serious issue with the contract builder. Check your custom contract type build file for errors.");
      }
    }
  }
}
