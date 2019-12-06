using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.LogicComponents.Spawners;

namespace MissionControl.EncounterFactories {
  public class LanceSpawnerFactory {
    public static LanceSpawnerGameLogic CreateLanceSpawner(GameObject parent, string name, string guid, string teamDefinitionGuid, bool spawnUnitsOnActivation,
      SpawnUnitMethodType spawnMethod, List<string> unitGuids) {

      GameObject lanceSpawnerGo = new GameObject(name);
      lanceSpawnerGo.transform.parent = parent.transform;
      lanceSpawnerGo.transform.localPosition = new Vector3(-674, 300, -280);

      LanceSpawnerGameLogic lanceSpawnerGameLogic = lanceSpawnerGo.AddComponent<LanceSpawnerGameLogic>();
      lanceSpawnerGameLogic.encounterObjectGuid = guid;
      lanceSpawnerGameLogic.teamDefinitionGuid = teamDefinitionGuid;
      lanceSpawnerGameLogic.spawnMethod = spawnMethod;
      lanceSpawnerGameLogic.spawnUnitsOnActivation = spawnUnitsOnActivation;

      float x = 0;
      float z = 0;
      for (int i = 0; i < unitGuids.Count; i++) {
        CreateUnitSpawnPoint(lanceSpawnerGo, $"UnitSpawnPoint{i + 1}", new Vector3(x, 0, z), unitGuids[i]);
        x += 24;
        z += 24;
      }

      lanceSpawnerGo.AddComponent<SnapToTerrain>();

      return lanceSpawnerGameLogic;
    }

    public static CustomPlayerLanceSpawnerGameLogic CreateCustomPlayerLanceSpawner(GameObject parent, string name, string guid, string teamDefinitionGuid, bool spawnUnitsOnActivation,
      SpawnUnitMethodType spawnMethod, List<string> unitGuids) {

      GameObject lanceSpawnerGo = new GameObject(name);
      lanceSpawnerGo.transform.parent = parent.transform;

      CustomPlayerLanceSpawnerGameLogic lanceSpawnerGameLogic = lanceSpawnerGo.AddComponent<CustomPlayerLanceSpawnerGameLogic>();
      lanceSpawnerGameLogic.encounterObjectGuid = guid;
      lanceSpawnerGameLogic.teamDefinitionGuid = teamDefinitionGuid;
      lanceSpawnerGameLogic.spawnMethod = spawnMethod;
      lanceSpawnerGameLogic.spawnUnitsOnActivation = spawnUnitsOnActivation;

      float x = 0;
      float z = 0;
      for (int i = 0; i < unitGuids.Count; i++) {
        CreateUnitSpawnPoint(lanceSpawnerGo, $"UnitSpawnPoint{i + 1}", new Vector3(x, 0, z), unitGuids[i]);
        x += 24;
        z += 24;
      }

      lanceSpawnerGo.AddComponent<SnapToTerrain>();

      return lanceSpawnerGameLogic;
    }

    public static PlayerLanceSpawnerGameLogic CreatePlayerLanceSpawner(GameObject parent, string name, string guid, string teamDefinitionGuid, bool spawnUnitsOnActivation,
      SpawnUnitMethodType spawnMethod, List<string> unitGuids) {

      GameObject lanceSpawnerGo = new GameObject(name);
      lanceSpawnerGo.transform.parent = parent.transform;

      PlayerLanceSpawnerGameLogic lanceSpawnerGameLogic = lanceSpawnerGo.AddComponent<PlayerLanceSpawnerGameLogic>();
      lanceSpawnerGameLogic.encounterObjectGuid = guid;
      lanceSpawnerGameLogic.teamDefinitionGuid = teamDefinitionGuid;
      lanceSpawnerGameLogic.spawnMethod = spawnMethod;
      lanceSpawnerGameLogic.spawnUnitsOnActivation = spawnUnitsOnActivation;

      float x = 0;
      float z = 0;
      for (int i = 0; i < unitGuids.Count; i++) {
        CreateUnitSpawnPoint(lanceSpawnerGo, $"UnitSpawnPoint{i + 1}", new Vector3(x, 0, z), unitGuids[i]);
        x += 24;
        z += 24;
      }

      lanceSpawnerGo.AddComponent<SnapToTerrain>();

      return lanceSpawnerGameLogic;
    }

    public static UnitSpawnPointGameLogic CreateUnitSpawnPoint(GameObject parent, string name, Vector3 localPosition, string encounterObjectGuid) {
      GameObject unitSpawnPointGo = new GameObject(name);
      unitSpawnPointGo.transform.parent = parent.transform;
      unitSpawnPointGo.transform.localPosition = localPosition;

      UnitSpawnPointGameLogic unitSpawnPoint = unitSpawnPointGo.AddComponent<UnitSpawnPointGameLogic>();
      unitSpawnPoint.defaultDetectionRange = 200f;
      unitSpawnPoint.encounterObjectGuid = encounterObjectGuid;

      unitSpawnPointGo.AddComponent<SnapToTerrain>();

      return unitSpawnPoint;
    }
  }
}