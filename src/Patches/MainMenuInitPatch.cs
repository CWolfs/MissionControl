using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.UI;

using MissionControl.Logic;

/*
  This patch sets the active contract type and starts any manipulation on the objectives in the game scene.
  This is called after: EncounterLayerParentFirstTimeInitializationPatch
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MainMenu), "Init")]
  public class MainMenuInitPatch {
    static void Prefix(MainMenu __instance) {
      Main.Logger.Log($"[MainMenuInitPatch Prefix] Patching Init");
      if (Main.Settings.DebugSkirmishMode) UiManager.Instance.SetupQuickSkirmishMenu();
    }
  }
}