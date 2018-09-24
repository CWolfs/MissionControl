using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public class DestroyBaseEncounterRules : EncounterRule {
    private GameObject PlotBase { get; set; }
    private List<string> ObjectReferenceQueue = new List<string>();

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

      int numberOfAdditionalLances = Main.Settings.AdditionalLances.SelectNumberOfAdditionalEnemyLances();
      for (int i = 0; i < numberOfAdditionalLances; i++) {
        int numberOfUnitsInLance = 4;
        string lanceGuid = Guid.NewGuid().ToString();
        List<string> unitGuids = GenerateGuids(numberOfUnitsInLance);
        string targetTeamGuid = TARGET_TEAM_ID;
        string spawnerName = $"Lance_Enemy_OpposingForce_{lanceGuid}";

        EncounterLogic.Add(new AddLanceToTargetTeam(lanceGuid, unitGuids));
        EncounterLogic.Add(new AddDestroyWholeUnitChunk(targetTeamGuid, lanceGuid, unitGuids, spawnerName, $"Destroy Pirate Support Lance {i + 1}"));
        EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, spawnerName, "PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 50f, 150f));

        ObjectReferenceQueue.Add(spawnerName);
      }
    }

    private void BuildSpawn() {
      Main.Logger.Log("[DestroyBaseEncounterRules] Building player spawn rule");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("PlotBase", GameObject.Find(GetPlotBaseName(mapName)));

      foreach (string objectName in ObjectReferenceQueue) {
        ObjectLookup.Add(objectName, GameObject.Find(objectName));    
      }
    }

    private string GetPlotBaseName(string mapName) {
      switch (mapName) {
        case "mapGeneral_theMound_vHigh":
          return "Ravine Position";
        default:
          return "Ravine Position";
      }
    }
  }
}