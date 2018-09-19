using UnityEngine;
using System;

using BattleTech;
using BattleTech.Designed;

namespace SpawnVariation.EncounterFramework {
  public class LanceSpawnerFactory {
    public static LanceSpawnerGameLogic CreateLanceSpawner(GameObject parent, string name, string teamDefinitionGuid, bool spawnUnitsOnActivation,
      SpawnUnitMethodType spawnMethod) {

      GameObject lanceSpawnerGo = new GameObject(name);
      lanceSpawnerGo.transform.parent = parent.transform;
      lanceSpawnerGo.transform.localPosition = Vector3.zero;

      LanceSpawnerGameLogic lanceSpawnerGameLogic = lanceSpawnerGo.AddComponent<LanceSpawnerGameLogic>();
      lanceSpawnerGameLogic.spawnMethod = spawnMethod;
      lanceSpawnerGameLogic.spawnUnitsOnActivation = spawnUnitsOnActivation;

      return lanceSpawnerGameLogic;
    }
  }
}