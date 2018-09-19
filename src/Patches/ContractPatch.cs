using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

namespace SpawnVariation.Patches {
  [HarmonyPatch(typeof(Contract), "BeginRequestResources")]
  public class ContractPatch {
    static void Postfix(Contract __instance, bool generateUnits) {
      if (generateUnits) {
        Main.Logger.Log($"[ContractPatch Postfix] Running postfix");
        PathFinderManager.GetInstance().RequestPathFinderMech();
      }
    }
  }
}