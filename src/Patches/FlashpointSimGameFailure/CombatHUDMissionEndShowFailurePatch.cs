using Harmony;

using BattleTech;
using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(CombatHUDMissionEnd), "ShowFailure")]
  public class CombatHUDMissionEndShowFailurePatch {
    static void Prefix(CombatHUDMissionEnd __instance, ref bool storyMission) {
        if ((MissionControl.Instance.IsAnyFlashpointContract() || UnityGameInstance.BattleTechGame.Simulation.IsInFlashpoint) && Main.Settings.NeverFailSimGameInFlashpoints) {
            storyMission = false;
        }
    }
  }
}