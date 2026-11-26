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
        UnityEngine.Random.InitState((int)(DateTime.Now.Ticks & 0x7FFFFFFF));
        UiManager.Instance.SetupQuickSkirmishMenu();
        UiManager.Instance.ShouldPatchMainMenu = false;
      }

      if (!DataManager.Instance.HasLoadedDeferredDefs) {
        DataManager.Instance.SubscribeDeferredDefs();
      }

      if (Main.Settings.VersionCheck) {
        // GUARD: If the github version cannot be obtained, ignore this check
        if (string.IsNullOrWhiteSpace(Main.Settings.GithubVersion) || Main.Settings.GithubVersion == "0.0.0") return;

        if (Main.Settings.Version != Main.Settings.GithubVersion) {
          if (TryParseVersion(Main.Settings.Version, out Version currentVersion, out bool isCurrentPrerelease) &&
            TryParseVersion(Main.Settings.GithubVersion, out Version latestVersion, out bool isLatestPrerelease)) {
            bool isNewer = currentVersion < latestVersion || (currentVersion == latestVersion && isCurrentPrerelease && !isLatestPrerelease);
            if (isNewer) {
              UiManager.Instance.ShowNewerVersionAvailablePopup(Main.Settings.Version, Main.Settings.GithubVersion);
            }
          }
        }
      }
    }

    private static bool TryParseVersion(string rawVersion, out Version version, out bool isPrerelease) {
      version = null;
      isPrerelease = false;
      if (string.IsNullOrWhiteSpace(rawVersion)) return false;

      string cleaned = rawVersion.Trim();
      int buildIndex = cleaned.IndexOf('+');
      if (buildIndex >= 0) cleaned = cleaned.Substring(0, buildIndex);

      int prereleaseIndex = cleaned.IndexOf('-');
      if (prereleaseIndex >= 0) {
        isPrerelease = true;
        cleaned = cleaned.Substring(0, prereleaseIndex);
      }

      return Version.TryParse(cleaned, out version);
    }
  }
}