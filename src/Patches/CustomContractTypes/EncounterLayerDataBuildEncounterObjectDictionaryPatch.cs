
using Harmony;

using BattleTech;

using System;
using System.Collections.Generic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerData), "BuildEncounterObjectDictionary")]
  [HarmonyPatch(new Type[] { typeof(Dictionary<string, EncounterObjectGameLogic>) })]
  public class EncounterLayerDataBuildEncounterObjectDictionaryPatch {
    static void Postfix(EncounterLayerData __instance, Dictionary<string, EncounterObjectGameLogic> encounterObjectDictionary) {
      Main.LogDebug($"[EncounterLayerDataBuildEncounterObjectDictionaryPatch.Prefix] Running EncounterLayerDataBuildEncounterObjectDictionaryPatch - {__instance.Name}");
      MissionControl.Instance.EncounterObjectDictionary = encounterObjectDictionary;
    }
  }
}