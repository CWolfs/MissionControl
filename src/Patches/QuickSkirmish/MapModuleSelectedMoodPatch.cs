using System.Collections.Generic;

using Harmony;

using BattleTech.Data;
using BattleTech.UI;
using BattleTech.Rendering.Mood;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MapModule), "get_SelectedMood")]
  public class MapModuleSelectedMoodPatch {
    public static BattleMood mood = null;

    static bool Prefix(MapModule __instance, ref BattleMood __result) {
      if (UiManager.Instance.ClickedQuickSkirmish) {
        Main.Logger.Log($"[MapModuleSelectedMoodPatch Prefix] Patching SelectedMood");
        if (mood == null) {
          List<Mood_MDD> moods = MetadataDatabase.Instance.GetMoods();
          int index = UnityEngine.Random.Range(0, moods.Count);
          Mood_MDD moodMdd = moods[index];
          mood = new BattleMood { Name = moodMdd.Name, FriendlyName = moodMdd.FriendlyName, Path = "MOOD_PATH_UNSET" };
        }
        
        __result = mood;
        return false;
      }

      return true;
    }
  }
}