using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class ArenaSkirmishEncounterRules : EncounterRules {
    public ArenaSkirmishEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[ArenaSkirmishEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[ArenaSkirmishEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAnywhere(this, "Player2LanceSpawner", "SpawnerPlayerLance"));
      EncounterLogic.Add(new SpawnLanceAroundTarget(this, "SpawnerPlayerLance", "Player2LanceSpawner", SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 150f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("Player2LanceSpawner", EncounterLayerData.gameObject.FindRecursive("Player2LanceSpawner"));
    }
  }
}