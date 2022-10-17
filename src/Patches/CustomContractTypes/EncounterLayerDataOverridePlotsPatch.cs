using Harmony;

using UnityEngine;

using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerData), "OverridePlots")]
  public class EncounterLayerDataOverridePlotsPatch {
    static bool Prefix(EncounterLayerData __instance) {
      if (MissionControl.Instance.IsCustomContractType) {
        Main.LogDebug($"[EncounterLayerData.Prefix] Running...");
        EncounterDataManager.Instance.HandleDeferredContractTypeBuild();
        return false;
      }
      return true;
    }
  }
}