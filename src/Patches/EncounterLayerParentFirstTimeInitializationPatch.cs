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
  This patch sets the active contract type and starts any manipulation on the objectives in the game scene.
  This is called before: EncounterLayerParentFirstTimeInitializationPatch
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerParent), "FirstTimeInitialization")]
  public class EncounterLayerParentFirstTimeInitializationPatch {
    static void Prefix(EncounterLayerParent __instance) {
      Main.Logger.Log($"[EncounterLayerParentFirstTimeInitializationPatch Prefix] Patching FirstTimeInitialization");
      MissionControl EncounterManager = MissionControl.Instance;
      if (EncounterManager.IsContractValid) EncounterManager.RunEncounterRules(LogicBlock.LogicType.SCENE_MANIPULATION);
    }
  }
}