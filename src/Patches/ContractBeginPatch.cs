using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

/*
  This patch is the start of this mod.
  It allows for the planning on what should be done for this particular encounter / contract.
  Once all the tasks are queued up they will be executed at the correct patch points
*/
namespace ContractCommand.Patches {
  [HarmonyPatch(typeof(Contract), "BeginRequestResources")]
  public class ContractBeginPatch {
    public static void Prefix(Contract __instance) {
      if (!__instance.Accepted) return;
      Main.Logger.Log($"[ContractBeginPatch Postfix] Patching Begin");
      Init(__instance);
    }

    public static void Init(Contract contract) {
      EncounterManager EncounterManager = EncounterManager.GetInstance();
      bool supportedContractType = EncounterManager.SetContractType(contract.ContractType);
    }
  }
}