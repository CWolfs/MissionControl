using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Harmony;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;


namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameState), "CompleteLanceConfigurationPrep")]
  public class SimGameStateCompleteLanceConfigurationPrepPatch {
    static void Prefix(SimGameState __instance) {
      Main.Logger.Log($"[SimGameStateCompleteLanceConfigurationPrepPatch Prefix] Patching Trigger");
      Contract contract = null;
      if (__instance.HasTravelContract) {
        contract = __instance.ActiveTravelContract;
      } else {
        contract = __instance.SelectedContract;
      }

      int maxNumberOfPlayerUnits = contract.Override.maxNumberOfPlayerUnits;
      List<UnitSpawnPointOverride> unitSpawnPointOverrideList = contract.Override.player1Team.lanceOverrideList[0].unitSpawnPointOverrideList;
      int unitCount = unitSpawnPointOverrideList.Count;

      Main.Logger.Log($"[SimGameStateCompleteLanceConfigurationPrepPatch Prefix] maxNumberOfPlayerUnits: {maxNumberOfPlayerUnits}");
      Main.Logger.Log($"[SimGameStateCompleteLanceConfigurationPrepPatch Prefix] unitSpawnPointOverrideList.count:  {unitCount}");

      if (unitCount < maxNumberOfPlayerUnits) {
        int diffUnits = maxNumberOfPlayerUnits - unitCount;
        Main.Logger.Log($"[SimGameStateCompleteLanceConfigurationPrepPatch Prefix] Diff: {diffUnits}");
        for (int i = 0; i < diffUnits; i++) {
          UnitSpawnPointOverride unitSpawnPointOverride = new UnitSpawnPointOverride();
          unitSpawnPointOverride.unitDefId = null;
          unitSpawnPointOverrideList.Add(unitSpawnPointOverride);
        }
      }
    }
  }
}