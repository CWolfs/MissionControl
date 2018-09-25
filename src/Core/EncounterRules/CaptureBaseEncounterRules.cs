using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class CaptureBaseEncounterRules : EncounterRules {
    public CaptureBaseEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[CaptureBaseEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[CaptureBaseEncounterRules] Building spawns rules");
      // EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "LanceEnemyOpposingForce", 400f));
    }

    public override void LinkObjectReferences(string mapName) {
      // ObjectLookup.Add("LanceEnemyOpposingForce", EncounterLayerGo.transform.FindRecursive("Lance_Enemy_OpposingForce").gameObject);
    }
  }
}