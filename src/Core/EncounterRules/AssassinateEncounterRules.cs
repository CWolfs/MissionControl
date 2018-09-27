using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class AssassinateEncounterRules : EncounterRules {
    public AssassinateEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[AssassinateEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[AssassinateEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "AssassinateSpawn"));
      EncounterLogic.Add(new SpawnLanceAnywhere(this, "AssassinateSpawn", "SpawnerPlayerLance", 400));
      EncounterLogic.Add(new LookAtTarget(this, "SpawnerPlayerLance", "AssassinateSpawn"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("AssassinateSpawn", EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_AssassinationTarget"));
    }
  }
}