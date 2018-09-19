using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

using SpawnVariation;

namespace SpawnVariation.Patches {
  [HarmonyPatch(typeof(ContractOverride), "GenerateUnits")]
  public class ContractOverrideGenerateUnitsPatch {
    static void Prefix(ContractOverride __instance, TeamOverride ___targetTeam) {
      Main.Logger.Log($"[ContractOveridePatch Prefix] Patching GenerateUnits");
      EncounterManager.GetInstance().AddLanceOverrideToTeamOverride(___targetTeam);
    }
  }
}