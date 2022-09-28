using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(CombatGameState), "OnCombatGameDestroyed")]
  public class CombatGameStateOnCombatDestroyedPatch {
    public static void Postfix() {
      MissionControl.Instance.OnCombatDestroyed();
    }
  }
}