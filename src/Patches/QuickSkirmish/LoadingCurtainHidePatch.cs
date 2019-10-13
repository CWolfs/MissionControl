using Harmony;

using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LoadingCurtain), "Hide")]
  public class LoadingCurtainHidePatch {
    static bool Prefix(LoadingCurtain __instance) {
      if (UiManager.Instance.ClickedQuickSkirmish) {
        Main.Logger.Log($"[LoadingCurtainHidePatch Prefix] Patching Hide");
        UiManager.Instance.ReadyToLoadQuickSkirmish = true;
        return false;
      }
      return true;
    }
  }
}