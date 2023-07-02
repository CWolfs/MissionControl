using Harmony;

using System.Linq;
using System.Collections.Generic;

using BattleTech;

namespace MissionControl.Patches {
  /**
  * This patch ensures that custom buildings bypass the minimum 8 cell raycast hit limit for being considered a valid building
  * If a custom building is being added it should be considered a valid building regardless of size
  */
  [HarmonyPatch(typeof(MapEncounterLayerDataCell), "AverageTheBuildingHits")]
  public class MapEncounterLayerDataCellAverageTheBuildingHitsPatch {
    static void Prefix(MapEncounterLayerDataCell __instance) {
      if (MissionControl.Instance.IsCustomContractType && MissionControl.Instance.CustomBuildingGuids.Count > 0) {
        if (__instance.tempBuildingCellHits == null) {
          __instance.buildingList = null;
          return;
        }

        foreach (List<BuildingRaycastHit> value in __instance.tempBuildingCellHits.Values) {
          BuildingRaycastHit buildingRaycastHit = value[0];

          if (MissionControl.Instance.CustomBuildingGuids.Contains(buildingRaycastHit.buildingGuid)) {
            Main.LogDebug($"[MapEncounterLayerDataCellAverageTheBuildingHitsPatch.Prefix] Force adding custom buildings to the building list - effectively bypassing the min 8 cell hit for a valid building with guid: " + value[0].buildingGuid);
            if (__instance.buildingList == null) {
              __instance.buildingList = new List<BuildingRaycastHit>();
            }

            for (int i = 1; i < value.Count; i++) {
              buildingRaycastHit.buildingHeight += value[i].buildingHeight;
              buildingRaycastHit.buildingSteepness += value[i].buildingSteepness;
            }

            buildingRaycastHit.buildingHeight /= value.Count;
            buildingRaycastHit.buildingSteepness /= value.Count;

            __instance.buildingList.Add(buildingRaycastHit);
          }
        }
      }
    }

    static void Postfix(MapEncounterLayerDataCell __instance) {
      if (MissionControl.Instance.IsCustomContractType && MissionControl.Instance.CustomBuildingGuids.Count > 0) {
        if (__instance.buildingList == null) {
          return;
        }


        // Remove duplicates
        var distinctList = __instance.buildingList
            .GroupBy(buildingRaycastHit => buildingRaycastHit.buildingGuid)
            .Select(group => group.First())
            .ToList();

        __instance.buildingList = distinctList;
      }
    }
  }
}