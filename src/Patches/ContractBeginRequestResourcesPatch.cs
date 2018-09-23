using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

/*
  This patch is at the right point to allow for requesting resources.
  This allows the system to load the resources ready for using them
  in the game scene.
*/
namespace EncounterCommand.Patches {
  [HarmonyPatch(typeof(Contract), "BeginRequestResources")]
  public class ContractBeginRequestResourcesPatch {
    static void Postfix(Contract __instance, bool generateUnits) {
      if (generateUnits) {
        Main.Logger.Log($"[ContractBeginRequestResourcesPatch Postfix] Patching BeginRequestResources");
        RequestUnits();
      }
    }

    static void RequestUnits(){
      PathFinderManager.GetInstance().RequestPathFinderMech();
    }
  }
}