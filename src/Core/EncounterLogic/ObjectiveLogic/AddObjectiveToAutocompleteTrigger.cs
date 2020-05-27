using BattleTech;
using BattleTech.Framework;
using BattleTech.AutoCompleteClasses;

namespace MissionControl.Logic {
  /*
  * Adds an objective to be checked as part of the Autocomplete logic
  * A trigger is what must be checked and align with the objective status on the trigger to be classed as a success
  */
  public class AddObjectiveToAutocompleteTrigger : ChunkLogic {
    private string objectiveId;

    public AddObjectiveToAutocompleteTrigger(string objectiveId) {
      this.objectiveId = objectiveId;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddObjectiveToAutocompleteTrigger] Adding objective '{objectiveId}' to AutocompleteGameLogic");
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      AutoCompleteGameLogic autoCompleteGameLogic = encounterLayerData.GetComponent<AutoCompleteGameLogic>();

      if (autoCompleteGameLogic != null) {
        TriggeringObjectiveStatus triggeringObjectiveStatus = new TriggeringObjectiveStatus();
        ObjectiveRef objectiveRef = new ObjectiveRef();
        objectiveRef.EncounterObjectGuid = objectiveId;
        triggeringObjectiveStatus.objective = objectiveRef;
        triggeringObjectiveStatus.objectiveStatus = ObjectiveStatusEvaluationType.Complete;

        autoCompleteGameLogic.triggeringObjectiveList.Add(triggeringObjectiveStatus);
      } else {
        Main.Logger.LogWarning($"[AddObjectiveToAutocompleteTrigger] Contract type '{MissionControl.Instance.CurrentContractType}' has no Autocomplete logic. Unable to add objective '{objectiveId}' to triggers");
      }
    }
  }
}