using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetStateAtRandomResult : EncounterResult {
    public List<string> EncounterGuids { get; set; } = new List<string>();
    public EncounterObjectStatus State { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetStateAtRandomResult] Setting state...");

      string encounterGuid = EncounterGuids.GetRandom();
      EncounterObjectGameLogic encounterGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterObjectGameLogic>(encounterGuid);

      if (encounterGameLogic != null) {
        Main.LogDebug($"[SetStateAtRandomResult] Setting '{encounterGameLogic.gameObject.name}' state '{State}'");
        encounterGameLogic.SetState(State);
      } else {
        Main.LogDebug($"[SetStateAtRandomResult] Cannot find EncounterObjectGameLogic with Guid '{encounterGuid}'");
      }
    }
  }
}
