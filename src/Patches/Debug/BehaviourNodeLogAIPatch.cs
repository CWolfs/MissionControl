using System;

using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(BehaviorNode), "LogAI")]
  [HarmonyPatch(new Type[] { typeof(AbstractActor), typeof(string), typeof(string) })]
  public class BehaviourNodeLogAIPatch {
    static bool Prefix(BehaviorNode __instance, string info) {
      if (!MissionControl.Instance.IsMCLoadingFinished) {
        return false;
      }
      return true;
    }
  }
}