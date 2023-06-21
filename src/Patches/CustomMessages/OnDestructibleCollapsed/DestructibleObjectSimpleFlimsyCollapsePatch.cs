using Harmony;

using BattleTech;

using MissionControl.Messages;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(DestructibleObject), "SimpleFlimsyCollapse")]
  public class DestructibleObjectSimpleFlimsyCollapsePatch {
    static void Prefix(DestructibleObject __instance, bool ___isCollapsed) {
      if (!___isCollapsed) {
        DestructibleCollapsedMessage destructibleCollapsedMessage = new DestructibleCollapsedMessage(__instance);
        EncounterLayerParent.EnqueueLoadAwareMessage(destructibleCollapsedMessage);
      }
    }
  }
}