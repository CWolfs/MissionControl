using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class CaptureEscortEncounterRules : EncounterRules {
    public CaptureEscortEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[CaptureEscortEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[CaptureEscortEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "EscortRegion"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("EscortRegion", EncounterLayerData.gameObject.FindRecursive("Region_Occupy"));
    }
  }
}