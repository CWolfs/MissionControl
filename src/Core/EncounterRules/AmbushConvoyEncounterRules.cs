using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class AmbushConvoyEncounterRules : EncounterRules {
    public AmbushConvoyEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[AmbushConvoyEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[AmbushConvoyEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "ConvoyUnit1Spawn"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("ConvoyUnit1Spawn", GameObject.Find("UnitSpawnPoint1"));
    }
  }
}