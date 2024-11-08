using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameState), "ParseContractActionData")]
  public class SimGameStateParseContractActionDataPatch {
    static void Prefix(SimGameState __instance, string actionValue, string[] additionalValues) {
      Main.Logger.Log($"[SimGameStateParseContractActionDataPatch Prefix] Patching");

      string customValue = additionalValues.Length >= 5 ? additionalValues[4] : "";

      switch (customValue) {
        case "UseExactLastUnits": {
          Main.Logger.Log($"[SimGameStateParseContractActionDataPatch Prefix] UseExactLastUnits");
          MissionControl.Instance.IsNextContractUseExactLastUnits = true;
          break;
        }
      }
    }
  }
}