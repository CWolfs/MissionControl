using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using SpawnVariation.Logic;

namespace SpawnVariation.Rules {
  public class DestroyBaseEncounterRules : EncounterRules {
    private GameObject PlotBase { get; set; }
    private GameObject LanceEnemyWave1 { get; set; }
    private GameObject LanceEnemyWave2 { get; set; }
    private GameObject LanceEnemyWave3 { get; set; } 

    public DestroyBaseEncounterRules() : base() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Setting up rule object references");
      PlotBase = GameObject.Find("Ravine Position");
    }

    public override void UpdateSpawns() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Updating spawns");
      UpdatePlayerLanceSpawn();
    }

    private void UpdatePlayerLanceSpawn() {
      int numberOfAdditionalEnemyLances = Main.Settings.numberOfAdditionalEnemyLances;
      Main.Logger.Log($"[DestroyBaseEncounterRules] Add {numberOfAdditionalEnemyLances} extra enemy lances");
      new SpawnLanceAtEdgeOfBoundary(SpawnerPlayerLanceGo, PlotBase);
    }
  }
}