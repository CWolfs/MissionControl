using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Logic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EmotePortrait), "LoadPortrait")]
  public class EmotePortraitLoadPortraitPatch {
    static void Postfix(EmotePortrait __instance, ref Sprite __result) {
      string path = Utilities.PathUtils.AppendPath(Main.Path, __instance.portraitAssetPath, false);
      if (File.Exists(path)) {
				__result = Utilities.ImageUtils.LoadSprite(path);
			}
    }
  }
}