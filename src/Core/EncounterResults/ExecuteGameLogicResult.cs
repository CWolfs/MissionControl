using UnityEngine;

using BattleTech;

using MissionControl.LogicComponents;

/**
	This result will execute a game logic that supports 'ExecutableGameLogic' interface
    - If ChunkGuid is provided it will scan all children on that chunk and execute _any_ ExecutableGameLogic
    - If EncounterGuid is provided it will try to find that EncounterObject and execute any ExecutableGameLogic on that object
*/
namespace MissionControl.Result {
  public class ExecuteGameLogicResult : EncounterResult {
    public string ChunkGuid { get; set; }
    public string EncounterGuid { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[ExecuteGameLogicResult] Executing Game Logic...");

      if (ChunkGuid != null) {
        EncounterChunkGameLogic chunkGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterChunkGameLogic>(ChunkGuid);
        if (chunkGameLogic != null) {
          MonoBehaviour[] monoBehaviours = chunkGameLogic.GetComponentsInChildren<MonoBehaviour>();
          foreach (MonoBehaviour monoBehaviour in monoBehaviours) {
            if (monoBehaviour is ExecutableGameLogic) {
              ((ExecutableGameLogic)monoBehaviour).Execute();
            }
          }
        } else {
          Main.LogDebug($"[ExecuteGameLogicResult] Cannot find ChunkGameLogic with Guid '{ChunkGuid}'");
        }
      } else if (EncounterGuid != null) {
        EncounterObjectGameLogic encounterGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<EncounterObjectGameLogic>(EncounterGuid);

        if (encounterGameLogic != null) {
          MonoBehaviour[] monoBehaviours = encounterGameLogic.GetComponents<MonoBehaviour>();
          foreach (MonoBehaviour monoBehaviour in monoBehaviours) {
            if (monoBehaviour is ExecutableGameLogic) {
              ((ExecutableGameLogic)monoBehaviour).Execute();
            }
          }
        } else {
          Main.LogDebug($"[ExecuteGameLogicResult] Cannot find EncounterObjectGameLogic with Guid '{EncounterGuid}'");
        }
      }
    }
  }
}
