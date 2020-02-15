using UnityEngine;

using BattleTech;

namespace MissionControl.Result {
  public class TagUnitsInRegionResult : EncounterResult {
    public string RegionGuid { get; set; }
    public int NumberOfUnits { get; set; } = 0;
    public string Type { get; set; }
    public string[] Tags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[TagUnitsInRegionResult] Tagging '{NumberOfUnits}' '{Type}' in region '{RegionGuid}'");
      TagUnitsInRegion();
    }

    private void TagUnitsInRegion() {
      int processedUnitCount = 0;
      RegionGameLogic regionGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[TagUnitsInRegionResult] Region Not Found for Guid '{RegionGuid}'");
        return;
      }

      // TODO: Add support for other units

      if (Type == "Building") {
        BuildingRepresentation[] buildingsInMap = GameObject.Find("GAME").GetComponentsInChildren<BuildingRepresentation>();
        Main.LogDebug($"[TagUnitsInRegionResult] Collected '{buildingsInMap.Length}' buildings to check.");

        if (NumberOfUnits > 0) {
          buildingsInMap.Shuffle();
        }

        foreach (BuildingRepresentation building in buildingsInMap) {
          bool isBuildingInRegion = RegionUtil.PointInRegion(UnityGameInstance.BattleTechGame.Combat, building.transform.position, RegionGuid);
          if (isBuildingInRegion) {
            Main.LogDebug($"[TagUnitsInRegionResult] Found building '{building.gameObject.name}' in region!");
            building.ParentBuilding.EncounterTags.UnionWith(Tags);
            processedUnitCount += 1;

            if (NumberOfUnits > 0 && (processedUnitCount >= NumberOfUnits)) {
              break;
            }
          }
        }
      } else {
        Main.LogDebug($"[TagUnitsInRegionResult] Tagging '{Type}' Not Yet Supported");
      }
    }
  }
}
