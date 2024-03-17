using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterObjectGameLogic), "EncounterStart")]
  public class EncounterObjectGameLogicEncounterStartPatch {
    static bool Prefix(EncounterObjectGameLogic __instance) {
      if (__instance.GetState() == EncounterObjectStatus.Finished) {
        Main.LogDebug($"[EncounterObjectGameLogicEncounterStartPatch] Preventing '{__instance.gameObject.name}' from setting a starting state because it's state is 'Finished'");
        __instance.encounterInitialized = true;
        return false;
      }

      return true;
    }
  }
}