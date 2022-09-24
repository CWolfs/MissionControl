using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetStatusAtRandomResult : EncounterResult {
    public List<string> EncounterGuids { get; set; } = new List<string>();
    public EncounterObjectStatus Status { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetStatusAtRandomResult] Setting status...");
      SetStatusAtRandom();
    }

    private void SetStatusAtRandom() {
      string encounterGuid = EncounterGuids.GetRandom();
      EncounterObjectGameLogic encounterGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterObjectGameLogic>(encounterGuid);

      if (encounterGameLogic != null) {
        // A chunk has been disabled by the contract override so ignore it and remove it from the list of choices
        if ((encounterGameLogic.StartingStatus == EncounterObjectStatus.ControlledByContract) && (encounterGameLogic.GetState() == EncounterObjectStatus.Finished)) {
          Main.LogDebug($"[SetStatusAtRandomResult] Avoiding '{encounterGameLogic.gameObject.name}' due to it not being an active chunk in the contract overrides");
          EncounterGuids.Remove(encounterGuid);
          SetStatusAtRandom();
        } else {
          Main.LogDebug($"[SetStatusAtRandomResult] Setting '{encounterGameLogic.gameObject.name}' status '{Status}'");
          encounterGameLogic.SetState(Status);
        }
      } else {
        Main.LogDebug($"[SetStatusAtRandomResult] Cannot find EncounterObjectGameLogic with Guid '{encounterGuid}'");
      }
    }
  }
}
