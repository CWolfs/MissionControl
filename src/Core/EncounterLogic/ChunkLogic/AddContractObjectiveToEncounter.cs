using System.Linq;

using BattleTech.Framework;

namespace MissionControl.Logic {
  public class AddContractObjectiveToEncounter : ChunkLogic {
    private string contractObjectiveOverrideGuid;

    public AddContractObjectiveToEncounter(string contractObjectiveOverrideGuid) {
      this.contractObjectiveOverrideGuid = contractObjectiveOverrideGuid;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddContractObjectiveToEncounter] Adding Contract Objective to Encounter");
      ContractObjectiveGameLogic contractObjectiveGameLogic = MissionControl.Instance.EncounterLayerData.gameObject.AddComponent<ContractObjectiveGameLogic>();
      ContractObjectiveOverride contractObjectiveOverride = MissionControl.Instance.CurrentContract.Override.contractObjectiveList.First(
        item => item.GUID == contractObjectiveOverrideGuid
      );

      contractObjectiveGameLogic.title = contractObjectiveOverride.title;
      contractObjectiveGameLogic.description = contractObjectiveOverride.description;
      contractObjectiveGameLogic.forPlayer = contractObjectiveOverride.forPlayer;
      contractObjectiveGameLogic.primary = contractObjectiveOverride.isPrimary;
      contractObjectiveGameLogic.encounterObjectGuid = contractObjectiveOverrideGuid;
    }
  }
}