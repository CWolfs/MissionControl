using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Config;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class DestroyBaseEncounterRules : EncounterRules {
    private GameObject PlotBase { get; set; }

    public DestroyBaseEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Setting up rule object references");
      BuildAdditionalLances();
      BuildSpawn();
    }

    private void BuildAdditionalLances() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Building additional lance rules");

      int numberOfAdditionalEnemyLances = Main.Settings.AdditionalLances.Enemy.SelectNumberOfAdditionalLances();
      for (int i = 0; i < numberOfAdditionalEnemyLances; i++) {
        new AddTargetLanceWithDestroyObjectiveBatch(this, "PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 50f, 150f, $"Destroy Pirate Support Lance {i + 1}");
      }

      int numberOfAdditionalAllyLances = Main.Settings.AdditionalLances.Allies.SelectNumberOfAdditionalLances();
      for (int i = 0; i < numberOfAdditionalAllyLances; i++) {
        new AddEmployerLanceBatch(this, "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 150f, 200f);
      }
    }

    private void BuildSpawn() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Building player spawn rule");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("PlotBase", GameObject.Find(GetPlotBaseName(mapName)));
    }
  }
}