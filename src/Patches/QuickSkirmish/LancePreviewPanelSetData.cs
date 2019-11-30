using Harmony;

using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LancePreviewPanel), "SetData")]
  public class LancePreviewPanelSetData {
    static bool Prefix(LancePreviewPanel __instance) {
      if (UiManager.Instance.ClickedQuickSkirmish) {
        Main.Logger.Log($"[LancePreviewPanelSetData Prefix] Patching SetData");
        return false;
      }
      return true;
    }
  }
}