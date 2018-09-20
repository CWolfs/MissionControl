using UnityEngine;
using System;

using BattleTech;
using BattleTech.Designed;

namespace SpawnVariation.EncounterFramework {
  public class LanceSpawnerFactory {
    public static LanceSpawnerGameLogic CreateLanceSpawner(GameObject parent, string name, string guid, string teamDefinitionGuid, bool spawnUnitsOnActivation,
      SpawnUnitMethodType spawnMethod) {

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
      for (int i = 0; i < 4; i++) {
        CreateUnitSpawnPoint(lanceSpawnerGo, $"UnitSpawnPoint{i + 1}", new Vector3(x, 0, z), EncounterManager.GetInstance().UnitGuids[i]);
        x += 25;
        z += 25;
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