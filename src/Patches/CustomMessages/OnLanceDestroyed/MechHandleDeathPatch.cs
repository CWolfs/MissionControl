using Harmony;

using BattleTech;

using MissionControl.Messages;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Mech), "HandleDeath")]
  public class MechHandleDeathPatch {
    static void Prefix(Mech __instance, out bool __state) {
      __state = __instance.IsFlaggedForDeath && !__instance.HasHandledDeath;
    }

    static void Postfix(Mech __instance, bool __state) {
      if (__state && __instance.HasHandledDeath && __instance.lance != null && __instance.lance.IsLanceDestroyed()) {
        LanceDestroyedMessage lanceDestroyedMessage = new LanceDestroyedMessage(__instance.lance.GUID.Replace(".Lance", ""), __instance.lance.GUID);
        EncounterLayerParent.EnqueueLoadAwareMessage(lanceDestroyedMessage);
      }
    }
  }
}