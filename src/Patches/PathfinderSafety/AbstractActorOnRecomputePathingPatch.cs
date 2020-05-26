using System;
using System.Collections.Generic;

using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(AbstractActor), "OnRecomputePathing")]
  public class AbstractActorOnRecomputePathingPatch {
    public static bool Prefix(AbstractActor __instance) {
      // Main.LogDebug($"[OnRecomputePathing] Actor is: {__instance.DisplayName}");
      if (MissionControl.Instance.IsMCLoadingFinished && (__instance.GUID.EndsWith(".9999999999") || __instance.GUID.EndsWith(".9999999998"))) {
        // Main.LogDebug($"[OnRecomputePathing] Blocking Actor");
        return false;
      }
      return true;
    }
  }
}