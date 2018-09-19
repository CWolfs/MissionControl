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
  
    public string Guid { get; set; }

    public static EncounterManager GetInstance() { 
      if (instance == null) instance = new EncounterManager();
      return instance;
    }

    private EncounterManager() {
      Init();
    }

    public void Init() {
      Guid = System.Guid.NewGuid().ToString();  // TODO: Temporary. Replace this with a lookup
    }

    public void AddLanceOverrideToTeamOverride(TeamOverride teamOverride) {
      List<LanceOverride> lanceOverrideList = teamOverride.lanceOverrideList;
      if (lanceOverrideList.Count > 0) {
        LanceOverride lanceOverride = lanceOverrideList[0].Copy();

        lanceOverride.name = "Lance_Enemy_OpposingForce_CWolf";
        
        LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef();
        lanceSpawnerRef.EncounterObjectGuid = Guid;
        lanceOverride.lanceSpawner = lanceSpawnerRef;
      } else {
        Main.Logger.LogError("[EncounterManager] Team Override has no lances available to copy. TODO: Generate new lance from stored JSON data");
      }
    }

    public void CreateDestroyWholeLanceObjective() {
      EncounterLayerData encounterLayerData = SpawnManager.GetInstance().EncounterLayerData;
      DestroyWholeLanceChunk destroyWholeChunk = ChunkFactory.CreateDestroyWholeLanceChunk();
      destroyWholeChunk.encounterObjectGuid = Guid;

      bool spawnOnActivation = true;
      LanceSpawnerGameLogic lanceSpawner = LanceSpawnerFactory.CreateLanceSpawner(
        destroyWholeChunk.gameObject,
        "Lance_Enemy_OpposingForce_CWolf",
        TARGET_TEAM_GUID,
        spawnOnActivation,
        SpawnUnitMethodType.InstantlyAtSpawnPoint
      );
    }
  }
}