using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using SpawnVariation.Logic;

namespace SpawnVariation.Rules {
  public class RescueEncounterRules : EncounterRules {
    private GameObject OccupyRegion1VIPGo { get; set; }

    public RescueEncounterRules() : base() {
      Main.Logger.Log("[RescueEncounterRules] Setting up rule object references");
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