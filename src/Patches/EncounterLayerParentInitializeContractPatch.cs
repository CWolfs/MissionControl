using Harmony;

using BattleTech;

using MissionControl.Logic;

/*
  This patch allows you to build custom objectives at the right point in the game logic
  This is called before: EncounterLayerParentFirstTimeInitializationPatch
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EncounterLayerParent), "InitializeContract")]
  public class EncounterLayerParentInitializeContractPatch {
    static void Prefix(EncounterLayerParent __instance) {
      Main.Logger.Log($"[EncounterLayerParentInitializeContractPatch Prefix] Patching InitializeContract");
      MissionControl.Instance.InitSceneData();
      MissionControl encounterManager = MissionControl.Instance;
      MissionControl.Instance.RunEncounterRules(LogicBlock.LogicType.ENCOUNTER_MANIPULATION);
    }
  }
}