using UnityEngine;

using BattleTech;
using BattleTech.Designed;

namespace MissionControl.EncounterFactories {
  public class ChunkFactory {
    public static DestroyWholeLanceChunk CreateDestroyWholeLanceChunk(string name = "Chunk_DestroyWholeLance", Transform parent = null) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject destroyWholeLanceChunkGo = new GameObject(name);
      destroyWholeLanceChunkGo.transform.parent = (parent != null) ? parent : encounterLayerGameObject.transform;
      destroyWholeLanceChunkGo.transform.localPosition = Vector3.zero;

      return destroyWholeLanceChunkGo.AddComponent<DestroyWholeLanceChunk>();
    }

    public static EmptyCustomChunkGameLogic CreateEmptyCustomChunk(string name) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject emptyCustomChunk = new GameObject(name);
      emptyCustomChunk.transform.parent = encounterLayerGameObject.transform;
      emptyCustomChunk.transform.localPosition = Vector3.zero;

      return emptyCustomChunk.AddComponent<EmptyCustomChunkGameLogic>();
    }

    public static DialogueChunkGameLogic CreateDialogueChunk(string name) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject dialogChunk = new GameObject(name);
      dialogChunk.transform.parent = encounterLayerGameObject.transform;
      dialogChunk.transform.localPosition = Vector3.zero;

      DialogueChunkGameLogic dialogueChunkGameLogic = dialogChunk.AddComponent<DialogueChunkGameLogic>();
      dialogueChunkGameLogic.encounterObjectName = name;
      return dialogueChunkGameLogic;
    }

    public static PlayerLanceChunkGameLogic CreatePlayerLanceChunk(string name = "Chunk_PlayerLance", Transform parent = null) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject playerLanceChunk = new GameObject(name);
      playerLanceChunk.transform.parent = (parent != null) ? parent : encounterLayerGameObject.transform;
      playerLanceChunk.transform.localPosition = Vector3.zero;

      PlayerLanceChunkGameLogic playerLanceChunkGameLogic = playerLanceChunk.AddComponent<PlayerLanceChunkGameLogic>();
      playerLanceChunkGameLogic.encounterObjectName = name;

      return playerLanceChunkGameLogic;
    }
  }
}