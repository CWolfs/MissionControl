using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(GameInstance), "LaunchContractFromSave")]
  public class GameInstanceLaunchContractFromSavePatch {
    public static void Postfix(GameInstance __instance, CombatGameState combat) {
      MissionControl.Instance.CurrentContract = combat.ActiveContract;
      MissionControl.Instance.SetFlashpointOverride();
    }
  }
}