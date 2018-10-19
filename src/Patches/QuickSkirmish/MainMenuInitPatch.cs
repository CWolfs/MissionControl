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
    public static bool HasPatchedMainMenu = false;

    static void Postfix(MainMenu __instance) {
      if (!HasPatchedMainMenu) {
        if (Main.Settings.DebugSkirmishMode) {
          Main.Logger.Log($"[MainMenuInitPatch Postfix] Patching Init");
          UnityEngine.Random.InitState(DateTime.Now.Millisecond);
          UiManager.Instance.SetupQuickSkirmishMenu();
        }

        DataManager.Instance.LoadVehicleDefs();
        HasPatchedMainMenu = true;
      }
    }
  }
}