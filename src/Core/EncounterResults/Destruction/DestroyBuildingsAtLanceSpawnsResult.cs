using UnityEngine;

using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class DestroyBuildingsAtLanceSpawnsResult : EncounterResult {
    public string LanceSpawnerGuid { get; set; }
    public float Radius { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[DestroyBuildingsAtLanceSpawnsResult] Setting state...");

      CombatGameState combat = UnityGameInstance.BattleTechGame?.Combat;
      if (combat == null) {
        Main.Logger.LogError("[DestroyBuildingsAtLanceSpawnsResult] Combat state is null.");
        return;
      }

      LanceSpawnerGameLogic lanceSpawnerGameLogic = combat.ItemRegistry.GetItemByGUID<LanceSpawnerGameLogic>(LanceSpawnerGuid);
      if (lanceSpawnerGameLogic == null) {
        Main.Logger.LogError($"[DestroyBuildingsAtLanceSpawnsResult] Could not find LanceSpawnerGameLogic for Guid '{LanceSpawnerGuid}'.");
        return;
      }

      UnitSpawnPointGameLogic[] unitSpawns = lanceSpawnerGameLogic.GetComponentsInChildren<UnitSpawnPointGameLogic>();

      List<BattleTech.Building> buildingsInMap = combat.ItemRegistry.GetObjectsOfType<BattleTech.Building>(TaggedObjectType.Building);
      Main.LogDebug($"[DestroyBuildingsAtLanceSpawnsResult] Collected '{buildingsInMap.Count}' buildings to check.");

      DestroyBuildingsUnderLanceSpawns(unitSpawns, buildingsInMap, Radius);
    }

    private void DestroyBuildingsUnderLanceSpawns(UnitSpawnPointGameLogic[] unitSpawns, List<BattleTech.Building> buildings, float radius) {
      HashSet<string> destroyedBuildingGuids = new HashSet<string>();

      foreach (UnitSpawnPointGameLogic unitSpawn in unitSpawns) {
        Vector3 position = unitSpawn.transform.position;

        foreach (BattleTech.Building building in buildings) {
          if (building == null || building.IsDead || destroyedBuildingGuids.Contains(building.GUID)) continue;

          BuildingRepresentation buildingRep = building.BuildingRep;
          if (buildingRep == null || !buildingRep.gameObject.activeInHierarchy) continue;

          if (position.DistanceFlat(buildingRep.transform.position) <= radius) {
            destroyedBuildingGuids.Add(building.GUID);
            EncounterLayerParent.EnqueueLoadAwareMessage(new DestroyActorMessage(EncounterLayerData.MapLogicGuid, building.GUID));
          }
        }
      }
    }
  }
}
