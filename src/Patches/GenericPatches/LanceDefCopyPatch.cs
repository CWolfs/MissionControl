using Harmony;

using BattleTech;

/*
  Vanilla bug fix: Copying a LanceDef doesn't maintain its Tagset 'tagSetSourceFile'. This fixes that bug.
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LanceDef), "Copy")]
  public class LanceOverrideCopyPatch {
    public static string cachedTagSetSourceFile;

    static void Prefix(LanceDef __instance) {
      cachedTagSetSourceFile = __instance.LanceTags.GetTagSetSourceFile();
    }

    static void Postfix(LanceDef __instance, LanceDef __result) {
      __result.LanceTags.SetTagSetSourceFile(cachedTagSetSourceFile);
      cachedTagSetSourceFile = null;
    }
  }
}