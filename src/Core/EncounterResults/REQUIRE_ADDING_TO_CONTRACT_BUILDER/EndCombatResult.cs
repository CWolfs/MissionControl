using BattleTech;

/**
	This result will end combat
*/
namespace MissionControl.Result {
  public class EndCombatRetreatResult : EncounterResult {
    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[EndCombatResult] Ending combat...");
      MissionRetreatMessage message = new MissionRetreatMessage(this.IsGoodFaithEffort());
      UnityGameInstance.BattleTechGame.MessageCenter.PublishMessage(message);
    }

    public bool IsGoodFaithEffort() {
      EncounterLayerData encounterLayerData = UnityEngine.Object.FindObjectOfType<EncounterLayerData>();
      return encounterLayerData.IsGoodFaithEffort;
    }
  }
}
