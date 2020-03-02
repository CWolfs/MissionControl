using UnityEngine;

using BattleTech;

namespace MissionControl.Result {
  public class DestroyBuildingsAtLanceSpawnsResult : EncounterResult {
    public string LanceSpawnerGuid { get; set; }
    public float Radius { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[DestroyBuildingsAtLanceSpawnsResult] Setting state...");

      LanceSpawnerGameLogic lanceSpawnerGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<LanceSpawnerGameLogic>(LanceSpawnerGuid);
      UnitSpawnPointGameLogic[] unitSpawns = lanceSpawnerGameLogic.GetComponentsInChildren<UnitSpawnPointGameLogic>();

      BuildingRepresentation[] buildingsInMap = GameObject.Find("GAME").GetComponentsInChildren<BuildingRepresentation>();
      Main.LogDebug($"[TagUnitsInRegionResult] Collected '{buildingsInMap.Length}' buildings to check.");

      DestroyBuildingsUnderLanceSpawns(unitSpawns, buildingsInMap, Radius);
    }

    private void DestroyBuildingsUnderLanceSpawns(UnitSpawnPointGameLogic[] unitSpawns, BuildingRepresentation[] buildings, float radius) {
      foreach (UnitSpawnPointGameLogic unitSpawn in unitSpawns) {
        Vector3 position = unitSpawn.transform.position;

        foreach (BuildingRepresentation building in buildings) {
          if (!building.IsDead) {
            if (position.DistanceFlat(building.transform.position) <= Radius) {
              EncounterLayerParent.EnqueueLoadAwareMessage(new DestroyActorMessage(EncounterLayerData.MapLogicGuid, building.parentCombatant.GUID));
            }
          }
        }
      }
    }
  }
}
