using Harmony;

using BattleTech;

using MissionControl.Interpolation;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(PlayerLanceSpawnerGameLogic), "SpawnUnits")]
  public class PlayerLanceSpawnerGameLogicSpawnUnitsPatch {
    public static void Postfix(PlayerLanceSpawnerGameLogic __instance) {
      PilotCastInterpolator.Instance.LateBinding();
    }
  }
}