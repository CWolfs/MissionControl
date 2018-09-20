using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

using SpawnVariation;

namespace SpawnVariation.Patches {
  // [HarmonyPatch(typeof(ContractOverride), "SafeGetEncounterObjectByGuid")]
  public class ContractOverrideSafeGetEncounterObjectByGuidPatch {
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
      Main.Logger.Log($"[ContractOverrideSafeGetEncounterObjectByGuidPatch Prefix] Patching SafeGetEncounterObjectByGuid");

      return instructions;
    }
  }
}