using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerParent), "InitFromSavePassTwo")]
  public class EncounterLayerParentInitFromSavePassTwoPatch {
    static void Postfix() {
      SaveSupport.CombatStatePersistence.RestoreDelays(UnityGameInstance.BattleTechGame.Simulation);
    }
  }
}
