using Harmony;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.EncounterNodes.Dropship;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerData), "ApplyContractOverride")]
  public class EncounterLayerDataApplyContractOverridePatch {
    static void Postfix(EncounterLayerData __instance, ContractOverride contractOverride) {
      Main.LogDebug($"[EncounterLayerDataApplyContractOverridePatch.Prefix] Running EncounterLayerDataApplyContractOverridePatch - {contractOverride.ID}");

    }

    private static void ApplyExtractionOverrides(ContractOverride contractOverride) {
      foreach (ExtractionOverride extractionOverride in contractOverride.extractionOverrideList) {
        CustomDropshipExtractionGameLogic dropshipExtractionChunkGameLogic = MissionControl.Instance.EncounterObjectDictionary[extractionOverride.GUID] as CustomDropshipExtractionGameLogic;

        if (dropshipExtractionChunkGameLogic != null) {
          dropshipExtractionChunkGameLogic.ApplyExtractionOverride(extractionOverride);
        } else {
          // TODO: Hijack the vanilla method to check/ignore the extractionOverride if it's going to be handled by the CustomDropshipExtractionGameLogic
          // Otherwise, vanilla will print a warning 
          EncounterLayerData.ShowMissingContractElementWarning("ExtractionOverride", extractionOverride.name, extractionOverride.GUID);
        }
      }
    }
  }
}