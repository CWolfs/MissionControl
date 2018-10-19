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
  [HarmonyPatch(typeof(LevelLoader), "LoadScene")]
  public class LevelLoaderLoadScenePatch {
    static void Prefix(LevelLoader __instance, string scene) {
      if (scene == "MainMenu") {
        UiManager.Instance.ShouldPatchMainMenu = true;
      }
    }
  }
}