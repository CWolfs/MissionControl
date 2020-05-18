using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(AbstractActor), "IsFriendly")]
  public class AbstractActorIsFriendlyPatch {
    public static bool Prefix(AbstractActor __instance, ICombatant target, ref bool __result) {
      if (__instance.team == null) {
        __result = false;
        return false;
      }
      return true;
    }
  }
}