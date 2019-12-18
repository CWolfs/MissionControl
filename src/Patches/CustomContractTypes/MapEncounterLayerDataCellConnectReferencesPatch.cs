using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MapEncounterLayerDataCell), "ConnectReferences")]
  public class MapEncounterLayerDataCellConnectReferencesPatch {
    static void Prefix(MapEncounterLayerDataCell __instance) {
      if (MissionControl.Instance.IsCustomContractType) {
        // Since we copy data from other encounters, they have dirty regions in them. Clear them so we're fresh.
        // TODO: When we add our regions for the custom type ensure we don't clear those
        if (__instance.regionGuidList != null) __instance.regionGuidList.Clear();
      }
    }
  }
}