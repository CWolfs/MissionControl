using UnityEngine;

using System.IO;

using Harmony;

using BattleTech;


namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EmotePortrait), "LoadPortrait")]
  public class EmotePortraitLoadPortraitPatch {
    public static bool Prefix(EmotePortrait __instance, ref Sprite __result) {
      if (__instance.portraitAssetPath.EndsWith(".generated")) {
        string pilotDefId = __instance.portraitAssetPath.Substring(0, __instance.portraitAssetPath.LastIndexOf("."));
        Sprite sprite = DataManager.Instance.GeneratedPortraits[pilotDefId];
        __result = sprite;
        return false;
      }

      return true;
    }

    public static void Postfix(EmotePortrait __instance, ref Sprite __result) {
      string path = Utilities.PathUtils.AppendPath(Main.Path, __instance.portraitAssetPath, false);
      if (File.Exists(path)) {
        __result = Utilities.ImageUtils.LoadSprite(path);
      }
    }
  }
}