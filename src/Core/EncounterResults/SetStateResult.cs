using BattleTech;

namespace MissionControl.Result {
  public class SetStateResult : EncounterResult {
    public string EncounterGuid { get; set; }
    public EncounterObjectStatus State { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetStateResult] Setting state...");

      EncounterObjectGameLogic encounterGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterObjectGameLogic>(EncounterGuid);

      if (encounterGameLogic != null) {
        if ((encounterGameLogic.StartingStatus == EncounterObjectStatus.ControlledByContract) && (encounterGameLogic.GetState() == EncounterObjectStatus.Finished)) {
          Main.LogDebug($"[SetStateResult] Avoiding '{encounterGameLogic.gameObject.name}' due to it not being an active chunk in the contract overrides");
        } else {
          Main.LogDebug($"[SetStateResult] Setting '{encounterGameLogic.gameObject.name}' state '{State}'");
          encounterGameLogic.SetState(State);
        }
      } else {
        Main.LogDebug($"[SetStateResult] Cannot find EncounterObjectGameLogic with Guid '{EncounterGuid}'");
      }
    }
  }
}
