using Harmony;

using BattleTech;
using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameInterruptManager), "QueueMechwarriorDeathEntry")]
  public class SimGameInterruptManagerQueueMechwarriorDeathEntryPatch {
    static bool Prefix(SimGameInterruptManager __instance) {
      Main.Logger.Log($"[SimGameInterruptManagerQueueMechwarriorDeathEntryPatch Prefix] Patching");

      if (MissionControl.Instance.IsNextContractUseExactLastUnits) {
        return false;
      }

      return true;
    }
  }
}