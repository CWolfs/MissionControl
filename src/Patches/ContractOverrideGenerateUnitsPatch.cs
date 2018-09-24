using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

using MissionControl;
using MissionControl.Logic;

/*
  This patch is used to inject a custom lance into the target team.
  This allows BT to then request the resources for the additional lance
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ContractOverride), "GenerateUnits")]
  public class ContractOverrideGenerateUnitsPatch {
    static void Prefix(ContractOverride __instance) {
      Main.Logger.Log($"[ContractOveridePatch Prefix] Patching GenerateUnits");
      RunPayload payload = new ContractOverridePayload(__instance);
      EncounterManager.GetInstance().RunEncounterRules(LogicBlock.LogicType.CONTRACT_OVERRIDE_MANIPULATION, payload);
    }
  }
}