using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using SpawnVariation.Logic;

namespace SpawnVariation.Rules {
  public class DestroyBaseEncounterRules : EncounterRule {
    private GameObject PlotBase { get; set; }

    public DestroyBaseEncounterRules() : base() {
      Build();
    }

    public void Build() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Building spawns rules");
      BuildPlayerLanceSpawn();
    }

    private void BuildPlayerLanceSpawn() {
      // int numberOfAdditionalEnemyLances = Main.Settings.numberOfAdditionalEnemyLances;
      // Main.Logger.Log($"[DestroyBaseEncounterRules] Add {numberOfAdditionalEnemyLances} extra enemy lances");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
    }

    public override void LinkObjectReferences() {
      ObjectLookup.Add("PlotBase", GameObject.Find("Ravine Position"));
    }
  }
}