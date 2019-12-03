using System;

using Harmony;

using BattleTech.UI;

using MissionControl;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MainMenu), "Init")]
  public class MainMenuInitPatch {
    static void Postfix(MainMenu __instance) {
      if (Main.Settings.DebugSkirmishMode && UiManager.Instance.ShouldPatchMainMenu) {
        Main.Logger.Log($"[MainMenuInitPatch Postfix] Patching Init");
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);
        UiManager.Instance.SetupQuickSkirmishMenu();
        UiManager.Instance.ShouldPatchMainMenu = false;
      }

      if (!DataManager.Instance.HasLoadedDeferredDefs) {
        DataManager.Instance.LoadDeferredDefs();
      }
    }
  }
}