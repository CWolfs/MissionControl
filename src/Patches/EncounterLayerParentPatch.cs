using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

namespace SpawnVariation {
  [HarmonyPatch(typeof(EncounterLayerParent), "FirstTimeInitialization")]
  public class EncounterLayerParentPatch {
    static void Prefix(EncounterLayerParent __instance) {
      Main.Logger.Log($"[EncounterLayerParentPatch Prefix] Running prefix");
      EncounterLayerData encounterLayerData =  __instance.GetSelectedEncounterLayerData();
      SpawnManager spawnManager = SpawnManager.GetInstance();
      bool supportedContractType = spawnManager.SetContractType(encounterLayerData.supportedContractType);
      if (supportedContractType) spawnManager.UpdateSpawns();
    }
  }
}