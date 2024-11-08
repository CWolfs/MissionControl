using Harmony;

using BattleTech;
using System.Collections.Generic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameState), "ShowMechRepairsNeededNotif")]
  public class SimGameStateShowMechRepairsNeededNotifPatch {
    static bool Prefix(SimGameState __instance) {
      Main.Logger.Log($"[SimGameStateShowMechRepairsNeededNotifPatch Prefix] Patching");

      if (MissionControl.Instance.IsNextContractUseExactLastUnits) {
        return false;
      }

      return true;
    }
  }
}