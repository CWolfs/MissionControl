using Harmony;

using BattleTech.Framework;

using MissionControl.Logic;

/*
  This patch is used to inject a custom lance into the target team.
  This allows BT to then request the resources for the additional lance
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ContractOverride), "GenerateUnits")]
  public class ContractOverrideGenerateUnitsPatch {
    static void Prefix(ContractOverride __instance) {
      Main.Logger.Log($"[ContractOveridePatch Prefix] Patching GenerateUnits");

      if (__instance.contract == null) __instance.SetupContract(MissionControl.Instance.CurrentContract);

      RunPayload payload = new ContractOverridePayload(__instance);
      MissionControl.Instance.RunEncounterRules(LogicBlock.LogicType.CONTRACT_OVERRIDE_MANIPULATION, payload);
    }
  }
}