using BattleTech;
using BattleTech.Data;
using BattleTech.Framework;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class TriggerResultAtRandomResult : EncounterResult {
    public List<DesignResult> Results { get; set; } = new List<DesignResult>();

    public override void AlwaysInit(CombatGameState combat) {
      base.AlwaysInit(combat);
      foreach (DesignResult result in Results) {
        result?.AlwaysInit(combat);
      }
    }

    public override void ContractInitialize() {
      base.ContractInitialize();
      foreach (DesignResult result in Results) {
        result?.ContractInitialize();
      }
    }

    public override void BeforeSave() {
      base.BeforeSave();
      foreach (DesignResult result in Results) {
        result?.BeforeSave();
      }
    }

    public override void WillLoadFromSave() {
      base.WillLoadFromSave();
      foreach (DesignResult result in Results) {
        result?.WillLoadFromSave();
      }
    }

    public override void EncounterResume(LoadRequest loadRequest) {
      base.EncounterResume(loadRequest);
      foreach (DesignResult result in Results) {
        result?.EncounterResume(loadRequest);
      }
    }

    public override void LoadComplete() {
      base.LoadComplete();
      foreach (DesignResult result in Results) {
        result?.LoadComplete();
      }
    }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[TriggerResultAtRandomResult] Triggering...");
      TriggerResultAtRandom(inMessage, triggeringName);
    }

    private void TriggerResultAtRandom(MessageCenterMessage inMessage, string triggeringName) {
      DesignResult result = Results.GetRandom();
      result?.Trigger(inMessage, triggeringName);
    }
  }
}
