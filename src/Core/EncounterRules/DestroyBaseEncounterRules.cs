using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using SpawnVariation.Logic;

namespace SpawnVariation.Rules {
  public class DestroyBaseEncounterRules : EncounterRule {
    private GameObject PlotBase { get; set; }

    public DestroyBaseEncounterRules() : base() {
      Build();
    }

    public void Build() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Setting up rule object references");
      BuildAdditionalLances();
      BuildSpawn();
    }

    private void BuildAdditionalLances() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Building additional lance rules");
      EncounterLogic.Add(new AddLanceToTargetLance());
      EncounterLogic.Add(new AddDestroyWholeUnitChunk());
    }

    private void BuildSpawn() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Building player spawn rule");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
    }

    public override void LinkObjectReferences() {
      ObjectLookup.Add("PlotBase", GameObject.Find("Ravine Position"));
    }
  }
}