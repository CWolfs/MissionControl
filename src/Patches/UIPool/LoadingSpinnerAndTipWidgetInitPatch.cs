using Harmony;

using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LoadingSpinnerAndTip_Widget), "Init")]
  public class LoadingSpinnerAndTipWidgetInitPatch {
    public static void Postfix(LoadingSpinnerAndTip_Widget __instance) {
      if (!UiManager.Instance.HasUI("MCUI_Contract_Type_Credits_Panel")) {
        UiManager.Instance.BuildContractTypeCreditsPrefab(__instance.gameObject);
      }
    }
  }
}