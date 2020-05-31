using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameState), "HasValidMaps")]
  public class SimGameStateHasValidMapsPatch {
    static void Postfix(SimGameState __instance, StarSystem system) {
      MissionControl.Instance.System = system;
    }
  }
}