using Harmony;

using BattleTech;
using BattleTech.Framework;

using System;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterChunkGameLogic), "ApplyContractOverride")]
  public class EncounterChunkGameLogicApplyContractOverridePatch {
    static bool Prefix(EncounterChunkGameLogic __instance, ChunkOverride chunkOverride) {
      Main.LogDebug($"[EncounterChunkGameLogicApplyContractOverridePatch.Prefix] Running EncounterChunkGameLogicApplyContractOverridePatch - {chunkOverride.name}");
      EncounterObjectStatus startingStatus = EncounterObjectStatus.Active;
      if (chunkOverride.enableChunkFromContract) {
        if (chunkOverride.controlledByContractChunkGroupList.Count > 0) {

          if (!Enum.TryParse(chunkOverride.controlledByContractChunkGroupList[0], out startingStatus)) {
            startingStatus = EncounterObjectStatus.Active;
          }
        }
        __instance.startingStatus = startingStatus;
      }
      return false;
    }
  }
}