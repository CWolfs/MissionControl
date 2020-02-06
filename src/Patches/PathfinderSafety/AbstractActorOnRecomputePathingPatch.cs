using System;
using System.Collections.Generic;

using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(AbstractActor), "OnRecomputePathing")]
  public class AbstractActorOnRecomputePathingPatch {
    public static void Prefix(AbstractActor __instance) {
      // Main.LogDebug($"[OnRecomputePathing] Actor is: {__instance.DisplayName}");
    }
  }
}