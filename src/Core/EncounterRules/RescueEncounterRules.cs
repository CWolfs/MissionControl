using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using SpawnVariation.Logic;

namespace SpawnVariation.Rules {
  public class RescueEncounterRules : EncounterRules {
    private GameObject EncounterLayerGo { get; set; }
    private GameObject ChunkPlayerLanceGo { get; set; }
    private GameObject SpawnerPlayerLanceGo { get; set; }
    private GameObject OccupyRegion1VIPGo { get; set; }

    public RescueEncounterRules() : base() {
      Main.Logger.Log("[RescueEncounterRules] Setting up rule object references");
      EncounterLayerGo = SpawnManager.GetInstance().EncounterLayerGameObject;
      ChunkPlayerLanceGo = EncounterLayerGo.transform.Find("Chunk_PlayerLance").gameObject;
      SpawnerPlayerLanceGo = ChunkPlayerLanceGo.transform.Find("Spawner_PlayerLance").gameObject;
      OccupyRegion1VIPGo = EncounterLayerGo.transform.Find("Chunk_OccupyRegion_1_VIP").gameObject;
    }

    public override void UpdateSpawns() {
      Main.Logger.Log("[RescueEncounterRules] Updating spawns");
      UpdatePlayerLanceSpawn();
    }

    private void UpdatePlayerLanceSpawn() {
      SpawnLogic logic = new SpawnLanceAtEdgeOfBoundary(SpawnerPlayerLanceGo, OccupyRegion1VIPGo);
    }
  }
}