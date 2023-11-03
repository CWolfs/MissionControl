using Harmony;

using BattleTech;

using MissionControl.Messages;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Turret), "HandleDeath")]
  public class TurretHandleDeathPatch {
    static void Postfix(Turret __instance) {
      if (__instance.lance.IsLanceDestroyed()) {
        LanceDestroyedMessage lanceDestroyedMessage = new LanceDestroyedMessage(__instance.lance.GUID.Replace(".Lance", ""), __instance.lance.GUID);
        EncounterLayerParent.EnqueueLoadAwareMessage(lanceDestroyedMessage);
      }
    }
  }
}