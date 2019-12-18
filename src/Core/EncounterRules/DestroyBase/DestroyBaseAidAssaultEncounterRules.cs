using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Config;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class DestroyBaseAidAssaultEncounterRules : EncounterRules {
    private GameObject PlotBase { get; set; }

    public DestroyBaseAidAssaultEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[DestroyBaseAidAssaultEncounterRules] Setting up rule object references");
      BuildRandomSpawn();
      BuildAdditionalLances("PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 150f, 250f);
    }

    private void BuildRandomSpawn() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[DestroyBaseAidAssaultEncounterRules] Building player spawn rule");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["PlotBase"] = GameObject.Find(GetPlotBaseName(mapName));
    }
  }
}