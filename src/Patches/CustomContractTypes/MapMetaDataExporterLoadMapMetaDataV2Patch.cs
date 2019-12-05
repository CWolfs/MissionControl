using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MapMetaDataExporter), "LoadMapMetaDataV2")]
  public class MapMetaDataExporterPatch {
    static bool Prefix(MapMetaDataExporter __instance, MapMetaData existingMapMetaData, string encounterLayerGuid, DataManager dataManager) {
      Main.Logger.Log($"[MapMetaDataExporterPatch Prefix] Patching LoadMapMetaDataV2");
      if (MissionControl.Instance.IsCustomContractType) {
        return false;
      }

      return true;
    }
  }
}