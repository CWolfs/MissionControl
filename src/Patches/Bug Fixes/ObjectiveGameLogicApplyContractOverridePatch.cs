using Harmony;

using BattleTech.Framework;

namespace MissionControl.Patches {
  // Vanilla Bug: When the ContractOverride is being applied into the ObjectiveGameLogic,
  // it does not add the isPrimary field of the ObjectiveOverride
  [HarmonyPatch(typeof(ObjectiveGameLogic), "ApplyContractOverride")]
  public static class ObjectiveGameLogicApplyContractOverridePatch {
    public static void Postfix(ObjectiveOverride objectiveOverride, ref bool ___primary) {
      if (Main.Settings.Misc.LanceSelectionDivergenceOverride.Enable) {
        ___primary = objectiveOverride.isPrimary;
      }
    }
  }
}
