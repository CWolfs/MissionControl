using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetStatusAtRandomResult : EncounterResult {
    public List<string> EncounterGuids { get; set; } = new List<string>();
    public EncounterObjectStatus Status { get; set; }

    private List<string> remainingGuids;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetStatusAtRandomResult] Setting status...");
      remainingGuids = new List<string>(EncounterGuids);
      SetStatusAtRandom();
    }

    private void SetStatusAtRandom() {
      if (remainingGuids.Count == 0) {
        Main.Logger.LogWarning("[SetStatusAtRandomResult] No valid encounter GUIDs remaining. All encounter objects are either ControlledByContract or invalid.");
        return;
      }

      string encounterGuid = remainingGuids.GetRandom();
      EncounterObjectGameLogic encounterGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterObjectGameLogic>(encounterGuid);

      if (encounterGameLogic != null) {
        // A chunk has been disabled by the contract override so ignore it and remove it from the list of choices
        if ((encounterGameLogic.StartingStatus == EncounterObjectStatus.ControlledByContract) && (encounterGameLogic.GetState() == EncounterObjectStatus.Finished)) {
          Main.LogDebug($"[SetStatusAtRandomResult] Avoiding '{encounterGameLogic.gameObject.name}' due to it not being an active chunk in the contract overrides");
          remainingGuids.Remove(encounterGuid);
          SetStatusAtRandom();
        } else {
          Main.LogDebug($"[SetStatusAtRandomResult] Setting '{encounterGameLogic.gameObject.name}' status '{Status}'");
          encounterGameLogic.SetState(Status);
        }
      } else {
        Main.Logger.LogWarning($"[SetStatusAtRandomResult] Cannot find EncounterObjectGameLogic with Guid '{encounterGuid}'. Status '{Status}' will not be applied for this chunk. This can be expected if the GUID belongs to a conditionally-disabled chunk or a chunk that hasn't been registered yet.");
      }
    }
  }
}
