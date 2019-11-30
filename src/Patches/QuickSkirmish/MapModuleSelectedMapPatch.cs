using System.Collections.Generic;

using Harmony;

using BattleTech.Data;
using BattleTech.UI;
using BattleTech.Framework;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MapModule), "get_SelectedMap")]
  public class MapModuleSelectedMapPatch {
    public static MapAndEncounters mapAndEncounter = null;

    static bool Prefix(MapModule __instance, ref MapAndEncounters __result) {
      if (UiManager.Instance.ClickedQuickSkirmish) {
        Main.Logger.Log($"[MapModuleSelectedMapPatch Prefix] Patching SelectedMap");
        if (mapAndEncounter == null) {
          List<MapAndEncounters> mapAndEncounters = MetadataDatabase.Instance.GetReleasedMapsAndEncountersByContractTypeAndOwnership((int)ContractType.ArenaSkirmish, false);
          int index = UnityEngine.Random.Range(0, mapAndEncounters.Count);
          mapAndEncounter = mapAndEncounters[index];
        }
        
        __result = mapAndEncounter;
        return false;
      }

      return true;
    }
  }
}