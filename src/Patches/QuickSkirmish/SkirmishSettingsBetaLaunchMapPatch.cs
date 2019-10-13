using Harmony;

using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SkirmishSettings_Beta), "LaunchMap")]
  public class SkirmishSettingsBetaLaunchMapPatch {
    static void Postfix() {
      if (UiManager.Instance.ClickedQuickSkirmish) {
        Main.Logger.Log($"[SkirmishSettingsBetaLaunchMapPatch Postfix] Patching LaunchMap");
        UiManager.Instance.ClickedQuickSkirmish = false;
        UiManager.Instance.ReadyToLoadQuickSkirmish = false;
        LoadingCurtain.Hide();
      }
    }
  }
}