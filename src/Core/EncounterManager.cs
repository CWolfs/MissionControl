using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using SpawnVariation.Logic;
using SpawnVariation.Rules;
using SpawnVariation.EncounterFramework;
using SpawnVariation.Utils;

namespace SpawnVariation {
  public class EncounterManager {
    private static EncounterManager instance;

    public const string TARGET_TEAM_GUID = "be77cadd-e245-4240-a93e-b99cc98902a5";
    public const string UNIT_1_SPAWNPOINT_GUID = "5ba11878-d123-469e-8d20-ab60743edf01";
    public const string UNIT_2_SPAWNPOINT_GUID = "2f66ecd0-6e1f-4583-a04d-f16da46c0026";
    public const string UNIT_3_SPAWNPOINT_GUID = "cf579a54-eab9-4100-a411-969c9fd57289";
    public const string UNIT_4_SPAWNPOINT_GUID = "cc992e8b-cde5-48a7-bcbf-bffbf1b0ff31";
  
    public string LanceGuid  { get; set; }
    public List<string> UnitGuids { get; private set; } = new List<string>();

    public static EncounterManager GetInstance() { 
      if (instance == null) instance = new EncounterManager();
      return instance;
    }

    private EncounterManager() {
      Init();
    }

    public void Init() {
      LanceGuid = System.Guid.NewGuid().ToString();  // TODO: Temporary. Replace this with a lookup
      UnitGuids.Add(UNIT_1_SPAWNPOINT_GUID);
      UnitGuids.Add(UNIT_2_SPAWNPOINT_GUID);
      UnitGuids.Add(UNIT_3_SPAWNPOINT_GUID);
      UnitGuids.Add(UNIT_4_SPAWNPOINT_GUID);
    }

    public void AddLanceOverrideToTeamOverride(TeamOverride teamOverride) {
      List<LanceOverride> lanceOverrideList = teamOverride.lanceOverrideList;
      if (lanceOverrideList.Count > 0) {
        LanceOverride lanceOverride = lanceOverrideList[0].Copy();

        lanceOverride.name = "Lance_Enemy_OpposingForce_CWolf";
        lanceOverride.unitSpawnPointOverrideList[0].unitSpawnPoint.EncounterObjectGuid = UNIT_1_SPAWNPOINT_GUID;
        lanceOverride.unitSpawnPointOverrideList[1].unitSpawnPoint.EncounterObjectGuid = UNIT_2_SPAWNPOINT_GUID;
        lanceOverride.unitSpawnPointOverrideList[2].unitSpawnPoint.EncounterObjectGuid = UNIT_3_SPAWNPOINT_GUID;
        lanceOverride.unitSpawnPointOverrideList[3].unitSpawnPoint.EncounterObjectGuid = UNIT_4_SPAWNPOINT_GUID;
        
        LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef();
        lanceSpawnerRef.EncounterObjectGuid = LanceGuid;
        lanceOverride.lanceSpawner = lanceSpawnerRef;

        teamOverride.lanceOverrideList.Add(lanceOverride);
      } else {
        Main.Logger.LogError("[EncounterManager] Team Override has no lances available to copy. TODO: Generate new lance from stored JSON data");
      }
    }

    public void CreateDestroyWholeLanceObjective() {
      EncounterLayerData encounterLayerData = SpawnManager.GetInstance().EncounterLayerData;
      DestroyWholeLanceChunk destroyWholeChunk = ChunkFactory.CreateDestroyWholeLanceChunk();
      destroyWholeChunk.encounterObjectGuid = System.Guid.NewGuid().ToString();

      bool spawnOnActivation = true;
      LanceSpawnerGameLogic lanceSpawner = LanceSpawnerFactory.CreateLanceSpawner(
        destroyWholeChunk.gameObject,
        "Lance_Enemy_OpposingForce_CWolf",
        LanceGuid,
        TARGET_TEAM_GUID,
        spawnOnActivation,
        SpawnUnitMethodType.InstantlyAtSpawnPoint
      );
      LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef(lanceSpawner);

      bool showProgress = true;
      int priority = -10;
      bool displayToUser = true;
      DestroyLanceObjective objective = ObjectiveFactory.CreateDestroyLanceObjective(
        destroyWholeChunk.gameObject,
        lanceSpawnerRef,
        "Destroy CWolf Guard Units",
        showProgress,
        "[percentageComplete]",
        "The primary objective to destroy the enemy lance",
        priority,
        displayToUser,
        ObjectiveMark.AttackTarget
      );

      
      DestroyLanceObjectiveRef destroyLanceObjectiveRef = new DestroyLanceObjectiveRef();
      destroyLanceObjectiveRef.encounterObject = objective;

      destroyWholeChunk.lanceSpawner = lanceSpawnerRef;
      destroyWholeChunk.destroyObjective = destroyLanceObjectiveRef;
    }
  }
}