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
    static void Prefix(MainMenu __instance) {
      Main.Logger.Log($"[MainMenuInitPatch Prefix] Patching Init");
      if (Main.Settings.DebugSkirmishMode) {
        UnityEngine.Random.InitState(DateTime.Now.Millisecond);
        UiManager.Instance.SetupQuickSkirmishMenu();
      }
    }
  }
}