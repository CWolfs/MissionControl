using UnityEngine;

using BattleTech;
using BattleTech.Designed;

namespace MissionControl.EncounterFactories {
  public class ChunkFactory {
    private static GameObject CreateGameObjectWithParent(string name, Transform parent = null) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject go = new GameObject(name);
      go.transform.parent = (parent != null) ? parent : encounterLayerGameObject.transform;
      go.transform.localPosition = Vector3.zero;
      return go;
    }

    public static DestroyWholeLanceChunk CreateDestroyWholeLanceChunk(string name = "Chunk_DestroyWholeLance", Transform parent = null) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject destroyWholeLanceChunkGo = CreateGameObjectWithParent(name, parent);

      return destroyWholeLanceChunkGo.AddComponent<DestroyWholeLanceChunk>();
    }

    public static EmptyCustomChunkGameLogic CreateEmptyCustomChunk(string name, Transform parent = null) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject emptyCustomChunk = CreateGameObjectWithParent(name, parent);

      return emptyCustomChunk.AddComponent<EmptyCustomChunkGameLogic>();
    }

    public static DialogueChunkGameLogic CreateDialogueChunk(string name, Transform parent = null) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject dialogChunk = CreateGameObjectWithParent(name, parent);

      DialogueChunkGameLogic dialogueChunkGameLogic = dialogChunk.AddComponent<DialogueChunkGameLogic>();
      dialogueChunkGameLogic.encounterObjectName = name;
      return dialogueChunkGameLogic;
    }

    public static PlayerLanceChunkGameLogic CreatePlayerLanceChunk(string name = "Chunk_PlayerLance", Transform parent = null) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject playerLanceChunk = CreateGameObjectWithParent(name, parent);

      PlayerLanceChunkGameLogic playerLanceChunkGameLogic = playerLanceChunk.AddComponent<PlayerLanceChunkGameLogic>();
      playerLanceChunkGameLogic.encounterObjectName = name;

      return playerLanceChunkGameLogic;
    }

    public static EncounterBoundaryChunkGameLogic CreateEncounterBondaryChunk(string name = "Chunk_EncounterBoundary", Transform parent = null) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject encounterBoundaryChunkGo = CreateGameObjectWithParent(name, parent);

      EncounterBoundaryChunkGameLogic encounterBoundaryChunkLogic = encounterBoundaryChunkGo.AddComponent<EncounterBoundaryChunkGameLogic>();
      encounterBoundaryChunkLogic.encounterObjectName = name;

      return encounterBoundaryChunkLogic;
    }
  }
}