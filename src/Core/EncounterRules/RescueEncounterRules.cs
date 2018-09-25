using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class RescueEncounterRules : EncounterRule {
    public RescueEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[RescueEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[RescueEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "OccupyRegion1VIPGo"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("OccupyRegion1VIPGo", EncounterLayerGo.transform.Find("Chunk_OccupyRegion_1_VIP").gameObject);
    }
  }
}