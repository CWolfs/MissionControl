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
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
      // TODO: Randomly spawn reinforcements X distance away from player unseen. Maybe a requiremnent for a new hook 
      // On the unit spawner on objective complete or timer complete
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("PlotBase", EncounterLayerData.gameObject.FindRecursive("Chunk_OccupyRegion_Base"));
    }
  }
}