using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class SimpleBattleEncounterRules : EncounterRule {
    public SimpleBattleEncounterRules() : base() {
      Build();
    }

    public void Build() {
      Main.Logger.Log("[SimpleBattleEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[SimpleBattleEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "LanceEnemyOpposingForce", 400f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("LanceEnemyOpposingForce", EncounterLayerGo.transform.FindRecursive("Lance_Enemy_OpposingForce").gameObject);
    }
  }
}