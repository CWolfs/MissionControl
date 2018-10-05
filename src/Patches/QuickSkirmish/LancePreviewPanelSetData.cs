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

/*
  This patch is used to inject a custom lance into the target team.
  This allows BT to then request the resources for the additional lance
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LancePreviewPanel), "SetData")]
  public class LancePreviewPanelSetData {
    static bool Prefix(LancePreviewPanel __instance) {
      Main.Logger.Log($"[LancePreviewPanelSetData Prefix] Patching SetData");
      if (UiManager.Instance.ClickedQuickSkirmish) return false;
      return true;
    }
  }
}