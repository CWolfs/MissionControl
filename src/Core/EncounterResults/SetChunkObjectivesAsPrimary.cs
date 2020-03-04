using BattleTech;

namespace MissionControl.Result {
  public class SetChunkObjectivesAsPrimary : EncounterResult {
    public string EncounterGuid { get; set; }
    public bool Primary { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetChunkObjectivesAsPrimary] Setting...");

      EncounterChunkGameLogic encounterChunkGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterChunkGameLogic>(EncounterGuid);

      if (encounterChunkGameLogic != null) {
        encounterChunkGameLogic.SetObjectivesAsPrimary(Primary);
      } else {
        Main.LogDebug($"[SetChunkObjectivesAsPrimary] Cannot find EncounterChunkGameLogic with Guid '{EncounterGuid}'");
      }
    }
  }
}
