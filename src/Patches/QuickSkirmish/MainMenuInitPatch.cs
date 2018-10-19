using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.UI;

using MissionControl.Logic;

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