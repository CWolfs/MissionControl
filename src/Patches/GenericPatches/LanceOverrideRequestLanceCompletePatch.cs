using Harmony;

using BattleTech.Framework;

using MissionControl.Logic;

/*
  This patch allows you to queue up any logic that requires to run at this LanceOverride.RequestLanceComplete Prefix and Postfix point
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LanceOverride), "RequestLanceComplete")]
  public class LanceOverrideRequestLanceCompletePatch {
    static void Prefix(LanceOverride __instance) {
      MissionControl encounterManager = MissionControl.Instance;
      RunPayload payload = new ContractAndLanceOverridePayload(encounterManager.CurrentContract.Override, __instance);

      MissionControl.Instance.RunEncounterRules(LogicBlock.LogicType.REQUEST_LANCE_COMPLETE, payload);
    }
  }
}