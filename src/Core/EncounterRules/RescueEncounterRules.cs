using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using SpawnVariation.Logic;

namespace SpawnVariation.Rules {
  public class RescueEncounterRules : EncounterRules {
    public RescueEncounterRules() : base() { }

    public override void UpdateSpawns() {
      Main.Logger.Log("[RescueEncounterRules] Updating spawns");
      UpdatePlayerLanceSpawn();
    }

    private void UpdatePlayerLanceSpawn() {
      GameObject encounterLayerGo = SpawnManager.GetInstance().EncounterLayerGameObject;
      GameObject chunkPlayerLance = encounterLayerGo.transform.Find("Chunk_PlayerLance").gameObject;
      GameObject spawnerPlayerLance = chunkPlayerLance.transform.Find("Spawner_PlayerLance").gameObject;
      SpawnLogic logic = new SpawnLanceAtEdgeOfBoundary(spawnerPlayerLance);
    }
  }
}