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
          // Vanilla AverageTheBuildingHits handles >= minBuildingHits; only handle custom buildings below that threshold so value[0] isn't double-averaged
          if (value == null || value.Count == 0 || value.Count >= MapEncounterLayerDataCell.minBuildingHits) {
            continue;
          }

          BuildingRaycastHit buildingRaycastHit = value[0];

          if (MissionControl.Instance.CustomBuildingGuids.Contains(buildingRaycastHit.buildingGuid)) {
            if (__instance.buildingList == null) {
              __instance.buildingList = new List<BuildingRaycastHit>();
            }

            if (!__instance.buildingList.Any(savedBuildingRaycastHit => savedBuildingRaycastHit.buildingGuid == buildingRaycastHit.buildingGuid)) {
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
    }
  }
}