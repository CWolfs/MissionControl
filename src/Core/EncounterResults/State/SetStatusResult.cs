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
          Main.LogDebug($"[SetStatusResult] Object is '{encounterGameLogic}' with state '{encounterGameLogic.GetState()}'");

          if (encounterGameLogic is EncounterChunkGameLogic encounterChunkGameLogic && encounterChunkGameLogic.IsState(EncounterObjectStatus.Finished)) {
            // If the encounter is finished it should not be reactivated - check if there are any objectives under it that are Ignored
            Main.LogDeveloperWarning($"[SetStatusResult] Setting the Chunk '{encounterGameLogic.gameObject.name}' Encounter Object state to '{Status}' but the Chunk status is 'Finished'. A 'Finished' Chunk should not be reactivated and will cause unexpected issues. This is often due to using the 'SetIgnoreChunk' result. You should only ignore chunks that are completely finished to prevent softlocks when trying to end a contract type. Or, it's a bug in your contract type. Check to see if your Trigger Conditionals are set correctly that run 'SetIgnoreChunk' results.");
          }

          // If the Objective is not active, we should not set it to anything other than 'Active'
          if (encounterGameLogic is ObjectiveGameLogic objectiveGameLogic && objectiveGameLogic.currentObjectiveStatus != ObjectiveStatus.Active) {
            Main.LogDeveloperWarning($"[SetStatusResult] Setting the Objective '{encounterGameLogic.gameObject.name}' Encounter Object state to '{Status}' but the Objective status is not 'Active' (these are different statuses). Current Objective status is '{objectiveGameLogic.currentObjectiveStatus}'. This will mean the Objective will not run.");

            // We should not set the Objective to 'Ignored' if it is not active
            if (objectiveGameLogic.currentObjectiveStatus == ObjectiveStatus.Ignored) Main.LogDeveloperWarning($"Do not set Chunks to 'Ignored' if you want to use them again. This sets all Objectives to 'Ignored'.");
          }

          encounterGameLogic.SetState(Status);
        }
      } else {
        Main.LogDebug($"[SetStatusResult] Cannot find EncounterObjectGameLogic with Guid '{EncounterGuid}'");
      }
    }
  }
}
