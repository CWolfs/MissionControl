using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class FireMissionEncounterRules : EncounterRules {
    public FireMissionEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[FireMissionEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[FireMissionEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAroundTarget(this, "SpawnerPlayerLance", "ChunkBeaconRegion1",
        SpawnLogic.LookDirection.TOWARDS_TARGET, 400, 600, true));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["ChunkBeaconRegion1"] = EncounterLayerData.gameObject.FindRecursive("Chunk_BeaconRegion_1");
    }
  }
}