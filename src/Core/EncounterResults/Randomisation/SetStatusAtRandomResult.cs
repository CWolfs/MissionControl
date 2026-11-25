using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetStatusAtRandomResult : EncounterResult {
    public List<string> EncounterGuids { get; set; } = new List<string>();
    public EncounterObjectStatus Status { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetStatusAtRandomResult] Setting status...");
      List<string> remainingGuids = new List<string>(EncounterGuids);

      while (remainingGuids.Count > 0) {
        string encounterGuid = remainingGuids.GetRandom();
        EncounterObjectGameLogic encounterGameLogic = !string.IsNullOrEmpty(encounterGuid)
          ? UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterObjectGameLogic>(encounterGuid)
          : null;

        if (encounterGameLogic != null) {
          // A chunk has been disabled by the contract override so ignore it and remove it from the list of choices
          if ((encounterGameLogic.StartingStatus == EncounterObjectStatus.ControlledByContract) && (encounterGameLogic.GetState() == EncounterObjectStatus.Finished)) {
            Main.LogDebug($"[SetStatusAtRandomResult] Avoiding '{encounterGameLogic.gameObject.name}' due to it not being an active chunk in the contract overrides");
            remainingGuids.Remove(encounterGuid);
            continue;
          }

          Main.LogDebug($"[SetStatusAtRandomResult] Setting '{encounterGameLogic.gameObject.name}' status '{Status}'");
          encounterGameLogic.SetState(Status);
          return;
        }

        Main.Logger.LogWarning($"[SetStatusAtRandomResult] Cannot find EncounterObjectGameLogic with Guid '{encounterGuid}'. Status '{Status}' will not be applied for this chunk. This can be expected if the GUID belongs to a conditionally-disabled chunk or a chunk that hasn't been registered yet.");
        remainingGuids.Remove(encounterGuid);
      }

      Main.Logger.LogWarning("[SetStatusAtRandomResult] No valid encounter GUIDs remaining. All encounter objects are either ControlledByContract or invalid.");
    }
  }
}
