using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Harmony;

using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using BattleTech.Framework;
using BattleTech.Rendering.Mood;

using MissionControl;
using MissionControl.Logic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MapModule), "get_SelectedMood")]
  public class MapModuleSelectedMoodPatch {
    static BattleMood mood = null;

    static bool Prefix(MapModule __instance, ref BattleMood __result) {
      Main.Logger.Log($"[MapModuleSelectedMoodPatch Prefix] Patching SelectedMood");

      if (UiManager.Instance.ClickedQuickSkirmish) {
        if (mood == null) {
          using (MetadataDatabase metadataDatabase = new MetadataDatabase()) {
            List<Mood_MDD> moods = metadataDatabase.GetMoods();
            int index = UnityEngine.Random.Range(0, moods.Count);
            Mood_MDD moodMdd = moods[index];
            mood = new BattleMood { Name = moodMdd.Name, FriendlyName = moodMdd.FriendlyName, Path = "MOOD_PATH_UNSET" };
          }
        }
        
        __result = mood;
        return false;
      }

      return true;
    }
  }
}