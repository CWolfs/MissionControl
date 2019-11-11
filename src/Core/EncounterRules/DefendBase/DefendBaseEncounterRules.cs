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
      BuildRandomSpawns();
      BuildAdditionalLances("SpawnerLanceEnemyWave1", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 50f, 250f);
    }

    public void BuildRandomSpawns() {
      if (!Main.Settings.RandomSpawns) return;
      
      Main.Logger.Log("[DefendBaseEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerPlayerLance", "PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 100f, 250f));
      EncounterLogic.Add(new SpawnLanceAroundTarget(this, "SpawnerLanceEnemyWave1", "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 400f, 600f, true));
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerLanceEnemyWave2", "PlotBase"));
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerLanceEnemyWave3", "PlotBase"));
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerLanceEnemyWave1", "SpawnerLanceEnemyWave1", "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 10f, 100f));
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerLanceEnemyWave2", "SpawnerLanceEnemyWave2", "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 10f, 100f));
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerLanceEnemyWave3", "SpawnerLanceEnemyWave3", "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 10f, 100f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["PlotBase"] = GameObject.Find(GetPlotBaseName(mapName));
      ObjectLookup["SpawnerLanceEnemyWave1"] = EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_Wave1Attackers");
      ObjectLookup["SpawnerLanceEnemyWave2"] = EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_Wave2Attackers");
      ObjectLookup["SpawnerLanceEnemyWave3"] = EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_Wave3Attackers");
    }
  }
}