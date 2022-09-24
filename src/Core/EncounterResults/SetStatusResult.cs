using BattleTech;

namespace MissionControl.Result {
  public class SetStatusResult : EncounterResult {
    public string EncounterGuid { get; set; }
    public EncounterObjectStatus Status { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetStatusResult] Setting state...");

      EncounterObjectGameLogic encounterGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterObjectGameLogic>(EncounterGuid);

      if (encounterGameLogic != null) {
        if ((encounterGameLogic.StartingStatus == EncounterObjectStatus.ControlledByContract) && (encounterGameLogic.GetState() == EncounterObjectStatus.Finished)) {
          Main.LogDebug($"[SetStatusResult] Avoiding '{encounterGameLogic.gameObject.name}' due to it not being an active chunk in the contract overrides");
        } else {
          Main.LogDebug($"[SetStatusResult] Setting '{encounterGameLogic.gameObject.name}' state '{Status}'");
          encounterGameLogic.SetState(Status);
        }
      } else {
        Main.LogDebug($"[SetStatusResult] Cannot find EncounterObjectGameLogic with Guid '{EncounterGuid}'");
      }
    }
  }
}
