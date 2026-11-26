using UnityEngine;

using System.IO;

using Harmony;

using BattleTech;


namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EmotePortrait), "LoadPortrait")]
  public class EmotePortraitLoadPortraitPatch {
    public static bool Prefix(EmotePortrait __instance, ref Sprite __result) {
      if (string.IsNullOrEmpty(__instance.portraitAssetPath)) return true;

      if (__instance.portraitAssetPath.EndsWith(".generated")) {
        string pilotDefId = __instance.portraitAssetPath.Substring(0, __instance.portraitAssetPath.LastIndexOf("."));

        if (DataManager.Instance.GeneratedPortraits.ContainsKey(pilotDefId)) {
          Sprite sprite = DataManager.Instance.GeneratedPortraits[pilotDefId];
          __result = sprite;
          return false;
        } else {
          Main.LogDebug("[EmotePortraitLoadPortraitPatch] Issue finding cached generated portrait. Skipping");
        }
      }

      return true;
    }

    public static void Postfix(EmotePortrait __instance, ref Sprite __result) {
      if (string.IsNullOrEmpty(__instance.portraitAssetPath)) return;

      // Portraits shipped in MissionControl or in any mod with an 'mcData' folder (e.g. 'sprites/Portraits/my_portrait.png')
      string path = DataManager.Instance.ResolveModFilePath(__instance.portraitAssetPath);
      if (path != null) {
        __result = Utilities.ImageUtils.LoadSprite(path);
      }
    }
  }
}