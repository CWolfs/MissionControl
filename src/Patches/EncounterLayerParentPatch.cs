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
      SpawnManager spawnManager = SpawnManager.GetInstance();
      spawnManager.SetContractType(ContractType.Rescue);
      spawnManager.UpdateSpawns();
    }
  }
}