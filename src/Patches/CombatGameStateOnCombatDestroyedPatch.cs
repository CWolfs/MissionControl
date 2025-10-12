using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
  public static class CombatGameStateOnCombatDestroyedPatch {
    public static void Postfix(CombatGameState __instance) {
      Main.Logger.Log("[CombatGameStateOnCombatDestroyedPatch] OnCombatGameDestroyed called - starting Mission Control cleanup");
      Main.Logger.Log($"[CombatGameStateOnCombatDestroyedPatch] Combat state: BattleTechGame={((__instance.BattleTechGame != null) ? "Valid" : "NULL")}, ActiveContract={((__instance.ActiveContract != null) ? "Valid" : "NULL")}");

      MissionControl.Instance.OnCombatDestroyed();

      Main.Logger.Log("[CombatGameStateOnCombatDestroyedPatch] Mission Control cleanup completed");
    }
  }
}