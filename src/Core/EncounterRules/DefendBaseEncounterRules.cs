using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using SpawnVariation.Logic;

namespace SpawnVariation.Rules {
  public class DefendBaseEncounterRules : EncounterRules {
    private GameObject PlotBase { get; set; }

    public DefendBaseEncounterRules() : base() {
      Main.Logger.Log("[DefendBaseEncounterRules] Setting up rule object references");
      PlotBase = GameObject.Find("Central Basin Base");
    }

    public override void UpdateSpawns() {
      Main.Logger.Log("[DefendBaseEncounterRules] Updating spawns");
      UpdatePlayerLanceSpawn();
    }

    private void UpdatePlayerLanceSpawn() {
      SpawnLogic logic = new SpawnLanceMembersAroundTarget(SpawnerPlayerLanceGo, PlotBase, SpawnLogic.LookDirection.AWAY_FROM_TARGET, 100f, 200f);
    }
  }
}