using Harmony;

using BattleTech;

using MissionControl.Messages;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(DestructibleObject), "ToggleInstantKillFlimsy")]
  public class DestructibleObjectToggleInstantKillFlimsyPatch {
    static void Prefix(bool ___isCollapsed, out bool __state) {
      __state = !___isCollapsed;
    }

    static void Postfix(DestructibleObject __instance, bool ___isCollapsed, bool __state) {
      if (__state && ___isCollapsed) {
        DestructibleCollapsedMessage destructibleCollapsedMessage = new DestructibleCollapsedMessage(__instance);
        EncounterLayerParent.EnqueueLoadAwareMessage(destructibleCollapsedMessage);
      }
    }
  }
}