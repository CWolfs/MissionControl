using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerParent), "InitFromSavePassOne")]
  public class EncounterLayerParentInitFromSavePassOnePatch {
    public static void Prefix(EncounterLayerParent __instance) {
      MissionControl.Instance.IsLoadingFromSave = true;
    }
  }
}