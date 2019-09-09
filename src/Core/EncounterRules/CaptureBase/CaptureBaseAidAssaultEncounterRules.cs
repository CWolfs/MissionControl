using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class CaptureBaseAidAssaultEncounterRules : EncounterRules {
    public CaptureBaseAidAssaultEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[CaptureBaseAidAssaultEncounterRules] Setting up rule object references");
      BuildSpawns();
      BuildAdditionalLances("PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 150f, 250f);
    }

    public void BuildSpawns() {
      Main.Logger.Log("[CaptureBaseAidAssaultEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["PlotBase"] = EncounterLayerData.gameObject.FindRecursive("Chunk_OccupyRegion_Base");
    }
  }
}