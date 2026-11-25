using Harmony;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.EncounterNodes.Dropship;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerData), "ApplyContractOverride")]
  public class EncounterLayerDataApplyContractOverridePatch {
    static void Postfix(EncounterLayerData __instance, ContractOverride contractOverride) {
      if (contractOverride == null) return;
      Main.LogDebug($"[EncounterLayerDataApplyContractOverridePatch.Postfix] Running EncounterLayerDataApplyContractOverridePatch - {contractOverride.ID}");
      ApplyExtractionOverrides(contractOverride);
    }

    private static void ApplyExtractionOverrides(ContractOverride contractOverride) {
      if (contractOverride.extractionOverrideList == null || MissionControl.Instance.EncounterObjectDictionary == null) return;

      foreach (ExtractionOverride extractionOverride in contractOverride.extractionOverrideList) {
        if (string.IsNullOrEmpty(extractionOverride.GUID)) continue;

        if (MissionControl.Instance.EncounterObjectDictionary.TryGetValue(extractionOverride.GUID, out EncounterObjectGameLogic encounterObject)) {
          CustomDropshipExtractionGameLogic dropshipExtractionChunkGameLogic = encounterObject as CustomDropshipExtractionGameLogic;
          if (dropshipExtractionChunkGameLogic != null) {
            dropshipExtractionChunkGameLogic.ApplyExtractionOverride(extractionOverride);
          }
        }
      }
    }
  }
}