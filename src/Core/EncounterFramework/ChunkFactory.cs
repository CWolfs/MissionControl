using UnityEngine;
using System;

using BattleTech.Designed;

namespace SpawnVariation.EncounterFramework {
  public class ChunkFactory {
    public static DestroyWholeLanceChunk CreateDestroyWholeLanceChunk() {
      GameObject encounterLayerGameObject = SpawnManager.GetInstance().EncounterLayerGameObject;
      GameObject destroyWholeLanceChunkGo = new GameObject("Chunk_DestroyWholeLance_CWolf");
      destroyWholeLanceChunkGo.transform.parent = encounterLayerGameObject.transform;
      destroyWholeLanceChunkGo.transform.localPosition = Vector3.zero;

      return destroyWholeLanceChunkGo.AddComponent<DestroyWholeLanceChunk>();
    }
  }
}