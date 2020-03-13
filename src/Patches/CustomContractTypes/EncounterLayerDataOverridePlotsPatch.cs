using Harmony;

using UnityEngine;

using BattleTech;

using System.Collections.Generic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerData), "OverridePlots")]
  public class EncounterLayerDataOverridePlotsPatch {
    static void Postfix(EncounterLayerData __instance) {
      Main.LogDebug($"[EncounterLayerData.Postfix] Running...");
      EncounterDataManager.Instance.HandleCustomContractType();
    }
  }
}