using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class DebugArenaSkirmishEncounterRules : EncounterRules {
    public DebugArenaSkirmishEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[DebugArenaSkirmishEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[DebugArenaSkirmishEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAnywhere(this, "Player2LanceSpawner", "SpawnerPlayerLance"));
      EncounterLogic.Add(new SpawnLanceAroundTarget(this, "SpawnerPlayerLance", "Player2LanceSpawner", SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 150f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("Player2LanceSpawner", EncounterLayerData.gameObject.FindRecursive("Player2LanceSpawner"));
    }
  }
}