using System;

using Harmony;

using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MainMenu), "Init")]
  public class MainMenuInitPatch {
    static void Postfix(MainMenu __instance) {
      UiManager.Instance.Init();

      if (Main.Settings.DebugSkirmishMode && UiManager.Instance.ShouldPatchMainMenu) {
        Main.Logger.Log($"[MainMenuInitPatch Postfix] Patching Init");
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);
        UiManager.Instance.SetupQuickSkirmishMenu();
        UiManager.Instance.ShouldPatchMainMenu = false;
      }

      if (!DataManager.Instance.HasLoadedDeferredDefs) {
        DataManager.Instance.SubscribeDeferredDefs();
      }

      if (Main.Settings.VersionCheck) {
        // GUARD: If the github version cannot be obtained, ignore this check
        if (Main.Settings.GithubVersion == "0.0.0") return;

        if (Main.Settings.Version != Main.Settings.GithubVersion) {
          Version currentVersion = new Version(Main.Settings.Version);
          Version latestVersion = new Version(Main.Settings.GithubVersion);
          if (currentVersion < latestVersion) {
            UiManager.Instance.ShowNewerVersionAvailablePopup(Main.Settings.Version, Main.Settings.GithubVersion);
          }
        }
      }
    }
  }
}