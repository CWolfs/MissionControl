using BattleTech.Framework;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class TriggerResultAtRandomResult : EncounterResult {
    public List<DesignResult> Results { get; set; } = new List<DesignResult>();

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[TriggerResultAtRandomResult] Triggering...");
      TriggerResultAtRandom();
    }

    private void TriggerResultAtRandom() {
      DesignResult result = Results.GetRandom();
      result.Trigger(null, null);
    }
  }
}
