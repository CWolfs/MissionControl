using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.UI;
using BattleTech.Save;

using MissionControl.Logic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SkirmishSettings_Beta), "LaunchMap")]
  public class SkirmishSettingsBetaLaunchMapPatch {
    static void Postfix() {
      if (UiManager.Instance.ClickedQuickSkirmish) {
        Main.Logger.Log($"[SkirmishSettingsBetaLaunchMapPatch Postfix] Patching LaunchMap");
        UiManager.Instance.ClickedQuickSkirmish = false;
        UiManager.Instance.ReadyToLoadQuickSkirmish = false;
        LoadingCurtain.Hide();
      }
    }
  }
}