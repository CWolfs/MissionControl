using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.UI;
using BattleTech.Framework;

using MissionControl;
using MissionControl.Logic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LancePreviewPanel), "SetData")]
  public class LancePreviewPanelSetData {
    static bool Prefix(LancePreviewPanel __instance) {
      if (UiManager.Instance.ClickedQuickSkirmish) {
        Main.Logger.Log($"[LancePreviewPanelSetData Prefix] Patching SetData");
        return false;
      }
      return true;
    }
  }
}