using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Result {
  public class TagUnitsInRegionResult : EncounterResult {
    public string RegionGuid { get; set; }
    public int NumberOfUnits { get; set; } = 0;
    public string Type { get; set; }
    public string[] Tags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      if (Tags == null || Tags.Length == 0) {
        Main.Logger.LogError($"[TagUnitsInRegionResult] No tags provided for region '{RegionGuid}'. Skipping.");
        return;
      }

      Main.LogDebug($"[TagUnitsInRegionResult] Tagging '{NumberOfUnits}' '{Type}' in region '{RegionGuid}' with tags '{string.Join(",", Tags)}'");
      TagUnitsInRegion();
    }

    private void TagUnitsInRegion() {
      CombatGameState combat = UnityGameInstance.BattleTechGame?.Combat;
      if (combat == null) {
        Main.Logger.LogError("[TagUnitsInRegionResult] Combat state is null.");
        return;
      }

      RegionGameLogic regionGameLogic = combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[TagUnitsInRegionResult] Region Not Found for Guid '{RegionGuid}'");
        return;
      }

      if (Type == "Building") {
        List<BattleTech.Building> buildingsInMap = combat.ItemRegistry.GetObjectsOfType<BattleTech.Building>(TaggedObjectType.Building);
        Main.LogDebug($"[TagUnitsInRegionResult] Collected '{buildingsInMap.Count}' buildings to check.");

        if (NumberOfUnits > 0) {
          buildingsInMap.Shuffle();
        }

        EncounterLayerData layerData = combat.EncounterLayerData ?? MissionControl.Instance.EncounterLayerData;
        int processedUnitCount = 0;
        foreach (BattleTech.Building building in buildingsInMap) {
          if (building == null || building.IsDead) continue;

          BuildingRepresentation buildingRep = building.BuildingRep;
          if (buildingRep == null || !buildingRep.gameObject.activeInHierarchy) continue;

          bool isBuildingInRegion = layerData.IsPointInRegion(buildingRep.transform.position, RegionGuid);
          if (isBuildingInRegion) {
            Main.LogDebug($"[TagUnitsInRegionResult] Found building '{buildingRep.gameObject.name}' in region!");
            building.EncounterTags.UnionWith(Tags);

            processedUnitCount += 1;
            if (NumberOfUnits > 0 && processedUnitCount >= NumberOfUnits) break;
          }
        }
      } else {
        Main.LogDebug($"[TagUnitsInRegionResult] Tagging '{Type}' Not Yet Supported");
      }
    }
  }
}
