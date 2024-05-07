using Harmony;

/*
  Prevents destructible objects from showing their shellInstance on damage as it causes z-fighting with the split
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(DestructibleObject), "TakeDamage")]
  public class DestructibleObjectTakeDamagePatch {
    public static void Postfix(DestructibleObject __instance) {
      if (MissionControl.Instance.IsCustomContractType) {
        Main.LogDebug("[DestructibleObjectTakeDamagePatch.Postfix] Running...");
        if (__instance.shellInstance != null) __instance.shellInstance.SetActive(false);
      }
    }
  }
}