using Harmony;

using BattleTech.Framework;

using MissionControl.Logic;

/*
  This patch allows you to queue up any logic that requires to run at ths LanceOverride.RequestLanceComplete Prefix and Postfix point
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LanceOverride), "RequestLanceComplete")]
  public class LanceOverrideRequestLanceCompletePatch {
    static void Prefix(LanceOverride __instance) {
      Main.Logger.Log($"[LanceOverrideRequestLanceCompletePatch Prefix] Patching");
      MissionControl encounterManager = MissionControl.Instance;
      MissionControl.Instance.RunEncounterRules(LogicBlock.LogicType.REQUEST_LANCE_COMPLETE);
    }
  }
}