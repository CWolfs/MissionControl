using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LevelLoader), "LoadScene")]
  public class LevelLoaderLoadScenePatch {
    static void Prefix(LevelLoader __instance, string scene) {
      if (Main.Settings.DebugSkirmishMode && scene == "MainMenu") {
        UiManager.Instance.ShouldPatchMainMenu = true;
      }
    }
  }
}