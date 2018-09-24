using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

using MissionControl.Logic;

/*
  This patch allows you to build custom objectives at the right point in the game logic
  This is called before: EncounterLayerParentFirstTimeInitializationPatch
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerParent), "InitializeContract")]
  public class EncounterLayerParentInitializeContractPatch {
    static void Prefix(EncounterLayerParent __instance) {
      Main.Logger.Log($"[EncounterLayerParentInitializeContractPatch Prefix] Patching InitializeContract");
      EncounterManager.GetInstance().InitSceneData();
      EncounterManager encounterManager = EncounterManager.GetInstance();
      EncounterManager.GetInstance().RunEncounterRules(LogicBlock.LogicType.ENCOUNTER_MANIPULATION);
    }
  }
}