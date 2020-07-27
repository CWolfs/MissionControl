using Harmony;

using System;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(GameInstance), "LaunchContract")]
  [HarmonyPatch(new Type[] { typeof(Contract) })]
  public class GameInstanceLaunchContractPatch {
    public static void Prefix(GameInstance __instance, Contract contract) {
      MissionControl.Instance.CurrentContract = contract;
      MissionControl.Instance.SetContractSettingsOverride();
    }
  }
}