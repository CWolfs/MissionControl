using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.UI;

using MissionControl;
using MissionControl.Logic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LoadingCurtain), "Hide")]
  public class LoadingCurtainHidePatch {
    static bool Prefix(LoadingCurtain __instance) {
      if (UiManager.Instance.ClickedQuickSkirmish) {
        Main.Logger.Log($"[LoadingCurtainHidePatch Prefix] Patching Hide");
        UiManager.Instance.ReadyToLoadQuickSkirmish = true;
        return false;
      }
      return true;
    }
  }
}