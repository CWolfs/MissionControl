using BattleTech;

using MissionControl.Data;

/**
	This result will end combat
*/
namespace MissionControl.Result {
  public class EndCombatRetreatResult : EncounterResult {
    public ContractRetreatEffort EffortOverride { get; set; } = ContractRetreatEffort.None;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[EndCombatResult] Ending combat...");
      bool isGoodFaithEffort = this.IsGoodFaithEffort();

      if (EffortOverride != ContractRetreatEffort.None) {
        isGoodFaithEffort = EffortOverride == ContractRetreatEffort.GoodFaith;
      }

      MissionRetreatMessage message = new MissionRetreatMessage(isGoodFaithEffort);
      UnityGameInstance.BattleTechGame.MessageCenter.PublishMessage(message);
    }

    public bool IsGoodFaithEffort() {
      EncounterLayerData encounterLayerData = UnityEngine.Object.FindObjectOfType<EncounterLayerData>();
      return encounterLayerData.IsGoodFaithEffort;
    }
  }
}
