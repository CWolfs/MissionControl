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

/*
  This patch sets the active contract type and starts any manipulation on the objectives in the game scene.
  This is called after: EncounterLayerParentFirstTimeInitializationPatch
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SkirmishSettings_Beta), "FinalizeLances")]
  public class SkirmishSettingsBetaFinalizeLancesPatch {
    public static string UNLIMITED_LANCE_COST = "999999999";
    public static string WAR_LANCE_COST = "25000000";
    public static string BATTLE_LANCE_COST = "20000000";
    public static string CLASH_LANCE_COST = "15000000";

    static bool Prefix(SkirmishSettings_Beta __instance, ref LanceConfiguration __result) {
      Main.Logger.Log($"[SkirmishSettingsBetaFinalizeLancesPatch Prefix] Patching FinalizeLances");
      if (UiManager.Instance.ClickedQuickSkirmish) {
        CloudUserSettings playerSettings = ActiveOrDefaultSettings.CloudSettings;
        LastUsedLances lastUsedLances = playerSettings.LastUsedLances;

        if (lastUsedLances.ContainsKey(UNLIMITED_LANCE_COST)) {
          __result = lastUsedLances[UNLIMITED_LANCE_COST]; 
        } else if (lastUsedLances.ContainsKey(WAR_LANCE_COST)) {
          __result = lastUsedLances[WAR_LANCE_COST]; 
        } else if (lastUsedLances.ContainsKey(BATTLE_LANCE_COST)) {
          __result = lastUsedLances[BATTLE_LANCE_COST]; 
        } else if (lastUsedLances.ContainsKey(CLASH_LANCE_COST)) {
          __result = lastUsedLances[CLASH_LANCE_COST]; 
        } else {
          Main.Logger.LogError("[Quick Skirmish] Quick Skirmish cannot be used without a prevously used lance. Go into skirmish and launch at least once");
        }
        return false;
      }
      return true;
    }
  }
}