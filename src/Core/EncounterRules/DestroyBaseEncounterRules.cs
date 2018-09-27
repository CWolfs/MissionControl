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
    private List<string> ObjectReferenceQueue = new List<string>();

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

      int numberOfAdditionalAllyLances = Main.Settings.AdditionalLances.Allies.SelectNumberOfAdditionalLances();
      for (int i = 0; i < numberOfAdditionalAllyLances; i++) {
        int numberOfUnitsInLance = 4;
        string lanceGuid = Guid.NewGuid().ToString();
        List<string> unitGuids = GenerateGuids(numberOfUnitsInLance);
        string employerTeamGuid = EMPLOYER_TEAM_ID;
        string spawnerName = $"Lance_Ally_SupportingForce_{lanceGuid}";

        EncounterLogic.Add(new AddLanceToAllyTeam(lanceGuid, unitGuids));
        EncounterLogic.Add(new AddLanceSpawnChunk(employerTeamGuid, lanceGuid, unitGuids, spawnerName, "Spawns a non-objective related ally supporting lance"));
        EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, spawnerName, "PlotBase", SpawnLogic.LookDirection.TOWARDS_TARGET, 150f, 200f));

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
        ObjectLookup.Add(objectName, EncounterLayerData.gameObject.FindRecursive(objectName));    
      }
    }
  }
}