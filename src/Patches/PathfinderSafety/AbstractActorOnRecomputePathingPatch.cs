using System;
using System.Collections.Generic;

using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(AbstractActor), "OnRecomputePathing")]
  public class AbstractActorOnRecomputePathingPatch {
    public static bool Prefix(AbstractActor __instance) {
      Main.LogDebug($"[OnRecomputePathing] Actor is: {__instance.DisplayName}");
      if (MissionControl.Instance.IsMCLoadingFinished && __instance.GUID.EndsWith(".9999999999")) {
        return false;
      }
      return true;
    }
  }
}