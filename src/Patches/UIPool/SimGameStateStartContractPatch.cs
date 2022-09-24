using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameState), "StartContract")]
  public class SimGameStateStartContractPatch {
    public static void Prefix(SimGameState __instance, Contract contract) {
      MissionControl.Instance.SetPreContractTypeInfo(contract);
    }
  }
}