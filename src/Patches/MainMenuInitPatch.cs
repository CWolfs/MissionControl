using System;
using System.Net;

using Harmony;

using BattleTech.UI;

using Newtonsoft.Json.Linq;

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

      if (Main.Settings.VersionCheck) {
        string modJson = new WebClient().DownloadString("https://raw.githubusercontent.com/CWolfs/MissionControl/master/mod.json");
        JObject json = JObject.Parse(modJson);
        string version = (string)json["Version"];

        if (Main.Settings.Version != version) {
          Version currentVersion = new Version(Main.Settings.Version);
          Version latestVersion = new Version(version);
          if (currentVersion < latestVersion) {
            UiManager.Instance.ShowNewerVersionAvailablePopup(Main.Settings.Version, version);
          }
        }
      }
    }
  }
}