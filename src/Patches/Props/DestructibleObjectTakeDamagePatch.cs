using Harmony;

using MissionControl.EncounterFactories;

/*
  Prevents custom MC destructible objects from showing their shellInstance on damage as it causes z-fighting with the split
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(DestructibleObject), "TakeDamage")]
  public class DestructibleObjectTakeDamagePatch {
    public static void Postfix(DestructibleObject __instance) {
      if (MissionControl.Instance.IsCustomContractType && PropFactory.IsCustomDestructionObject(__instance.shellInstance)) {
        Main.LogDebug("[DestructibleObjectTakeDamagePatch.Postfix] Running...");
        __instance.shellInstance.SetActive(false);
      }
    }
  }
}