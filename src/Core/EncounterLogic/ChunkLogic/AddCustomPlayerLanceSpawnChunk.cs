using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.EncounterFactories;
using MissionControl.LogicComponents.Spawners;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class AddCustomPlayerLanceSpawnChunk : ChunkLogic {
    private string teamGuid;
    private string lanceGuid;
    private List<string> unitGuids;
    private string spawnerName;
    private string debugDescription;

    public AddCustomPlayerLanceSpawnChunk(string teamGuid, string lanceGuid, List<string> unitGuids, string spawnerName, string debugDescription) {
      this.teamGuid = teamGuid;
      this.lanceGuid = lanceGuid;
      this.unitGuids = unitGuids;
      this.spawnerName = spawnerName;
      this.debugDescription = debugDescription;
    }
    
    public AddCustomPlayerLanceSpawnChunk(string lanceGuid, List<string> unitGuids, string spawnerName, string debugDescription) {
      this.lanceGuid = lanceGuid;
      this.unitGuids = unitGuids;
      this.spawnerName = spawnerName;
      this.debugDescription = debugDescription;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddCustomPlayerLanceSpawnChunk] Adding encounter structure");
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      EmptyCustomChunkGameLogic emptyCustomChunk = ChunkFactory.CreateEmptyCustomChunk("Chunk_Lance");
      emptyCustomChunk.encounterObjectGuid = System.Guid.NewGuid().ToString();
      emptyCustomChunk.notes = debugDescription;

      // Auto select Player or Employer based on the lance configurator
      if (teamGuid == null) {
        SpawnableUnit[] lanceUnits = MissionControl.Instance.CurrentContract.Lances.GetLanceUnits(EncounterRules.PLAYER_TEAM_ID);
        if (lanceUnits.Length > 4) {
          teamGuid = EncounterRules.PLAYER_TEAM_ID;
        } else {
          teamGuid = EncounterRules.EMPLOYER_TEAM_ID;
        }
      }

      // Guard: Don't do anything if there are no employer units and employer mode is on
      SpawnableUnit[] employerLanceUnits = MissionControl.Instance.CurrentContract.Lances.GetLanceUnits(EncounterRules.EMPLOYER_TEAM_ID);
      Main.Logger.Log($"[AddCustomPlayerLanceExtraSpawnPoints] '{employerLanceUnits.Length}' employer lance units are being sent to Mission Control by Bigger Drops.");
      if (employerLanceUnits.Length <= 0) return;

      bool spawnOnActivation = true;
      CustomPlayerLanceSpawnerGameLogic lanceSpawner = LanceSpawnerFactory.CreateCustomPlayerLanceSpawner(
        emptyCustomChunk.gameObject,
        spawnerName,
        lanceGuid,
        teamGuid,
        spawnOnActivation,
        SpawnUnitMethodType.ViaLeopardDropship,
        unitGuids
      );
      lanceSpawner.transform.position = Vector3.zero;
    }
  }
}