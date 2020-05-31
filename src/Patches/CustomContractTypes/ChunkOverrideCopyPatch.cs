using Harmony;

using BattleTech;
using BattleTech.Framework;

using System;
using System.Collections.Generic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ChunkOverride), "Copy")]
  public class ChunkOverrideCopyPatch {
    static bool Prefix(ChunkOverride __instance, ref ChunkOverride __result) {
      // Main.LogDebug($"[ChunkOverrideCopyPatch.Prefix] Running ChunkOverrideCopyPatch - {__instance.name}");
      __result = new ChunkOverride {
        name = __instance.name,
        encounterChunk = new EncounterChunkRef(__instance.encounterChunk),
        enableChunkFromContract = __instance.enableChunkFromContract,
        requirementList = Utilities.Cloner.DeepCopy<List<RequirementDef>>(__instance.requirementList),
        controlledByContractChunkGroupList = __instance.controlledByContractChunkGroupList
      };
      return false;
    }
  }
}