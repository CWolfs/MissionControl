using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class BattleEncounterRules : EncounterRule {
    public BattleEncounterRules() : base() {
      Build();
    }

    public void Build() {
      Main.Logger.Log("[BattleEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[BattleEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "OccupyRegion1VIPGo"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("OccupyRegion1VIPGo", EncounterLayerGo.transform.Find("Chunk_OccupyRegion_1_VIP").gameObject);
    }
  }
}