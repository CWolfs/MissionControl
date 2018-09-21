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
      int numberOfUnitsInLance = 4;
      string lanceGuid = Guid.NewGuid().ToString();
      List<string> unitGuids = GenerateGuids(numberOfUnitsInLance);
      string targetTeamGuid = EMPLOYER_TEAM_ID;

      EncounterLogic.Add(new AddLanceToTargetLance(lanceGuid, unitGuids));
      EncounterLogic.Add(new AddDestroyWholeUnitChunk(targetTeamGuid, lanceGuid, unitGuids));
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