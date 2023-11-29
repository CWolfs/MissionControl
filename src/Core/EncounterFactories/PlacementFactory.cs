using UnityEngine;

using BattleTech;

using MissionControl.EncounterNodes.Placers;

namespace MissionControl.EncounterFactories {
  public class PlacementFactory {
    private static GameObject CreateGameObject(GameObject parent, string name = null) {
      GameObject go = new GameObject((name == null) ? "SwapPlacement" : name);
      go.transform.parent = parent.transform;
      go.transform.localPosition = Vector3.zero;

      return go;
    }

    public static SwapPlacementGameLogic CreateSwapSpawn(GameObject parent, string name, string guid, string targetGuid1, string targetGuid2) {
      GameObject spawnSwapGameObject = CreateGameObject(parent, name);

      SwapPlacementGameLogic swapSpawnGameLogic = spawnSwapGameObject.AddComponent<SwapPlacementGameLogic>();
      swapSpawnGameLogic.encounterObjectGuid = guid;
      swapSpawnGameLogic.swapTarget1Guid = targetGuid1;
      swapSpawnGameLogic.swapTarget2Guid = targetGuid2;

      return swapSpawnGameLogic;
    }
  }
}