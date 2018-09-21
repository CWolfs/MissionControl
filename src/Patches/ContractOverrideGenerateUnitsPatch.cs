using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.Framework;

using SpawnVariation;

/*
  This patch is used to inject a custom lance into the target team.
  This allows BT to then request the resources for the additional lance
*/
namespace SpawnVariation.Patches {
  [HarmonyPatch(typeof(ContractOverride), "GenerateUnits")]
  public class ContractOverrideGenerateUnitsPatch {
    static void Prefix(ContractOverride __instance, TeamOverride ___targetTeam) {
      Main.Logger.Log($"[ContractOveridePatch Prefix] Patching GenerateUnits");
      EncounterManager.GetInstance().AddLanceOverrideToTeamOverride(___targetTeam);
    }
  }
}