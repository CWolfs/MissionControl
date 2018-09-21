using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

/*
  This patch sets the active contract type and starts any manipulation on the objectives in the game scene.
  This is called after: EncounterLayerParentFirstTimeInitializationPatch
*/
namespace SpawnVariation.Patches {
  [HarmonyPatch(typeof(EncounterLayerParent), "FirstTimeInitialization")]
  public class EncounterLayerParentFirstTimeInitializationPatch {
    static void Prefix(EncounterLayerParent __instance) {
      Main.Logger.Log($"[EncounterLayerParentFirstTimeInitializationPatch Prefix] Patching FirstTimeInitialization");
      SpawnManager spawnManager = SpawnManager.GetInstance();
      if (spawnManager.IsContractValid) spawnManager.RunEncounterRules();
    }
  }
}