using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace SpawnVariation.Rules {
  public abstract class EncounterRules {
    protected GameObject EncounterLayerGo { get; set; }
    protected GameObject ChunkPlayerLanceGo { get; set; }
    protected GameObject SpawnerPlayerLanceGo { get; set; }

    public EncounterRules() {
      EncounterLayerGo = SpawnManager.GetInstance().EncounterLayerGameObject;
      ChunkPlayerLanceGo = EncounterLayerGo.transform.Find("Chunk_PlayerLance").gameObject;
      SpawnerPlayerLanceGo = ChunkPlayerLanceGo.transform.Find("Spawner_PlayerLance").gameObject;
    }

    public abstract void UpdateSpawns();
  }
}