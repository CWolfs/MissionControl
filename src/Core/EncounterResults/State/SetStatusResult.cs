using BattleTech;
using BattleTech.Framework;

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

          if (encounterGameLogic is ObjectiveGameLogic objectiveGameLogic && objectiveGameLogic.currentObjectiveStatus != ObjectiveStatus.Active) {
            Main.Logger.LogWarning($"[SetStatusResult] Setting the Objective '{encounterGameLogic.gameObject.name}' Encounter Object state to '{Status}' but the Objective status is not 'Active' (these are different statuses). Current Objective status is '{objectiveGameLogic.currentObjectiveStatus}'. This will mean the Objective will not run.");
            if (objectiveGameLogic.currentObjectiveStatus == ObjectiveStatus.Ignored) Main.Logger.LogWarning($"Do not set Chunks to 'Ignored' if you want to use them again. This sets all Objectives to 'Ignored'.");
          }

          encounterGameLogic.SetState(Status);
        }
      } else {
        Main.LogDebug($"[SetStatusResult] Cannot find EncounterObjectGameLogic with Guid '{EncounterGuid}'");
      }
    }
  }
}
