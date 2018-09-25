using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class DefendBaseEncounterRules : EncounterRules {
    private GameObject PlotBase { get; set; }

    public DefendBaseEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[DefendBaseEncounterRules] Setting up rule object references");
      BuildSpawns();
    }

    public void BuildSpawns() {
      Main.Logger.Log("[DefendBaseEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerPlayerLance", "PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 100f, 200f));
      EncounterLogic.Add(new SpawnLanceAroundTarget(this, "SpawnerLanceEnemyWave1", "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 300f, 500f));
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerLanceEnemyWave2", "PlotBase"));
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerLanceEnemyWave3", "PlotBase"));
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerLanceEnemyWave1", "SpawnerLanceEnemyWave1", "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 200f));
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerLanceEnemyWave2", "SpawnerLanceEnemyWave2", SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 200f));
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerLanceEnemyWave3", "SpawnerLanceEnemyWave3", SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 200f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("PlotBase", GameObject.Find(GetPlotBaseName(mapName)));
      ObjectLookup.Add("SpawnerLanceEnemyWave1", GameObject.Find("Lance_Enemy_Wave1Attackers"));
      ObjectLookup.Add("SpawnerLanceEnemyWave2", GameObject.Find("Lance_Enemy_Wave2Attackers"));
      ObjectLookup.Add("SpawnerLanceEnemyWave3", GameObject.Find("Lance_Enemy_Wave3Attackers"));
    }
  }
}