using UnityEngine;

using BattleTech;

using MissionControl.LogicComponents;

/**
	This result will execute a game logic that supports 'ExecutableGameLogic' interface
    - If ChunkGuid is provided it will scan all children on that chunk and execute _any_ ExecutableGameLogic
    - If EncounterGuid is provided it will try to find that EncounterObject and execute any ExecutableGameLogic on that object
*/
namespace MissionControl.Result {
  public class SetStateResult : EncounterResult {
    public string EncounterGuid { get; set; }
    public EncounterObjectStatus State { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[SetStateResult] Setting state...");

      EncounterObjectGameLogic encounterGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterObjectGameLogic>(EncounterGuid);

      if (encounterGameLogic != null) {
        Main.LogDebug($"[SetStateResult] Setting '{encounterGameLogic.gameObject.name}' state '{State}'");
        encounterGameLogic.SetState(State);
      } else {
        Main.LogDebug($"[SetStateResult] Cannot find EncounterObjectGameLogic with Guid '{EncounterGuid}'");
      }
    }
  }
}
