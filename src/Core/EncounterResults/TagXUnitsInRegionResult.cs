using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using System.Collections.Generic;

using Harmony;

/**
	This result will reposition a region within a min and max threshold. 
	It will also recreate the Mesh to match the terrain for triggering the region correctly
*/
namespace MissionControl.Result {
  public class TagXUnitsInRegionResult : EncounterResult {
    public string RegionGuid { get; set; }
    public int NumberOfUnits { get; set; } = 1;
    public string Type { get; set; }
    public string[] Tags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[TagXUnitsInRegionResult] Tagging '{NumberOfUnits}' '{Type}' in region '{RegionGuid}'");
      TagUnitsInRegion();
    }

    private void TagUnitsInRegion() {
      RegionGameLogic regionGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[TagXUnitsInRegionResult] Region Not Found for Guid '{RegionGuid}'");
        return;
      }

      if (Type == "Building") {
        BuildingRepresentation[] buildingsInMap = GameObject.Find("GAME").GetComponentsInChildren<BuildingRepresentation>();
        Main.LogDebug($"[TagXUnitsInRegionResult] Collected '{buildingsInMap.Length}' buildings to check.");

        foreach (BuildingRepresentation building in buildingsInMap) {
          bool isBuildingInRegion = RegionUtil.PointInRegion(UnityGameInstance.BattleTechGame.Combat, building.transform.position, RegionGuid);
          if (isBuildingInRegion) {
            Main.LogDebug($"[TagXUnitsInRegionResult] Found building '{building.gameObject.name}' in region!");
            building.ParentBuilding.EncounterTags.UnionWith(Tags);
          }
        }
      } else {
        Main.LogDebug($"[TagXUnitsInRegionResult] Tagging '{Type}' Not Yet Supported");
      }
    }
  }
}
