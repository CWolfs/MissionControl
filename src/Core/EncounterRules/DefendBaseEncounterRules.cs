using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using SpawnVariation.Logic;

namespace SpawnVariation.Rules {
  public class DefendBaseEncounterRules : EncounterRules {
    private GameObject PlotBase { get; set; }
    private GameObject LanceEnemyWave1 { get; set; }
    private GameObject LanceEnemyWave2 { get; set; }
    private GameObject LanceEnemyWave3 { get; set; } 

    public DefendBaseEncounterRules() : base() {
      Main.Logger.Log("[DefendBaseEncounterRules] Setting up rule object references");
      PlotBase = GameObject.Find("Central Basin Base");
      LanceEnemyWave1 = GameObject.Find("Lance_Enemy_Wave1Attackers");
      LanceEnemyWave2 = GameObject.Find("Lance_Enemy_Wave2Attackers");
      LanceEnemyWave3 = GameObject.Find("Lance_Enemy_Wave3Attackers");
    }

    public override void UpdateSpawns() {
      Main.Logger.Log("[DefendBaseEncounterRules] Updating spawns");
      UpdatePlayerLanceSpawn();
    }

    private void UpdatePlayerLanceSpawn() {
      new SpawnLanceMembersAroundTarget(SpawnerPlayerLanceGo, PlotBase, SpawnLogic.LookDirection.AWAY_FROM_TARGET, 100f, 200f);
      new SpawnLanceAroundTarget(LanceEnemyWave1, PlotBase, SpawnLogic.LookDirection.TOWARDS_TARGET, 300f, 500f);
      new SpawnLanceAtEdgeOfBoundary(LanceEnemyWave2, PlotBase);
      new SpawnLanceAtEdgeOfBoundary(LanceEnemyWave3, PlotBase);
      new SpawnLanceMembersAroundTarget(LanceEnemyWave1, LanceEnemyWave1, PlotBase, SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 200f);
      new SpawnLanceMembersAroundTarget(LanceEnemyWave2, LanceEnemyWave2, SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 200f);
      new SpawnLanceMembersAroundTarget(LanceEnemyWave3, LanceEnemyWave3, SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 200f);
    }
  }
}