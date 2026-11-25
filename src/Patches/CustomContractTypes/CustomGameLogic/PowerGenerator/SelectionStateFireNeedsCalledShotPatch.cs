using Harmony;

using BattleTech;
using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SelectionStateFire), "NeedsCalledShot", MethodType.Getter)]
  public static class SelectionStateFireNeedsCalledShotPatch {
    public static void Postfix(SelectionStateFire __instance, ref bool __result) {
      if (__result && __instance.TargetedCombatant is Turret) __result = false;
    }
  }
}
