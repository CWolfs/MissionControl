using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetStateAtRandomResult : EncounterResult {
    public List<string> EncounterGuids { get; set; } = new List<string>();
    public EncounterObjectStatus State { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetStateAtRandomResult] Setting state...");
      SetStateAtRandom();
    }

    private void SetStateAtRandom() {
      string encounterGuid = EncounterGuids.GetRandom();
      EncounterObjectGameLogic encounterGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterObjectGameLogic>(encounterGuid);

      if (encounterGameLogic != null) {
        // A chunk has been disabled by the contract override so ignore it and remove it from the list of choices
        if ((encounterGameLogic.StartingStatus == EncounterObjectStatus.ControlledByContract) && (encounterGameLogic.GetState() == EncounterObjectStatus.Finished)) {
          Main.LogDebug($"[SetStateAtRandomResult] Avoiding '{encounterGameLogic.gameObject.name}' due to it not being an active chunk in the contract overrides");
          EncounterGuids.Remove(encounterGuid);
          SetStateAtRandom();
        } else {
          Main.LogDebug($"[SetStateAtRandomResult] Setting '{encounterGameLogic.gameObject.name}' state '{State}'");
          encounterGameLogic.SetState(State);
        }
      } else {
        Main.LogDebug($"[SetStateAtRandomResult] Cannot find EncounterObjectGameLogic with Guid '{encounterGuid}'");
      }
    }
  }
}
