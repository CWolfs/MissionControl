using BattleTech;

using System.Collections.Generic;

/**
	This result will go throug the chunk guids provided and set all objectives in them to Ignore
*/
namespace MissionControl.Result {
  public class IgnoreChunksResult : EncounterResult {
    public List<string> EncounterGuids { get; set; } = new List<string>();

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[IgnoreChunksResult] Triggering");
      IgnoreChunks();
    }

    private void IgnoreChunks() {
      foreach (string encounterGuid in EncounterGuids) {
        EncounterChunkGameLogic encounterChunkGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterChunkGameLogic>(encounterGuid);

        if (encounterChunkGameLogic != null) {
          encounterChunkGameLogic.SetObjectivesAsIgnored();
          encounterChunkGameLogic.SetState(EncounterObjectStatus.Finished);
        } else {
          Main.LogDebug($"[SetChunkObjectivesAsPrimary] Cannot find EncounterChunkGameLogic with Guid '{encounterGuid}'");
        }
      }
    }
  }
}
