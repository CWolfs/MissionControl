using Harmony;

using BattleTech;

using System.Collections;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Team), "DelayEnemySpottedVO")]
  public class TeamDelayEnemySpottedVOPatch {
    static bool Prefix(Team __instance, AbstractActor actor, ref IEnumerator __result) {
      Main.LogDebug($"[TeamDelayEnemySpottedVOPatch] Running Prefix");
      if (__instance.IsFriendly(actor.team) || __instance.IsNeutral(actor.team)) {
        Main.LogDebug($"[TeamDelayEnemySpottedVOPatch] Spotted team unit is friendly or neutral. No need to trigger enemy spotted audio.");
        __result = EmptyEnumerator();
        return false;
      }
      return true;
    }

    static IEnumerator EmptyEnumerator() {
      yield break;
    }
  }
}