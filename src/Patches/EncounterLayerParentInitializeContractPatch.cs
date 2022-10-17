using Harmony;

using BattleTech;

using MissionControl.Logic;
using MissionControl.Interpolation;

/*
  This patch allows you to build custom objectives at the right point in the game logic
  At this point in the game all units that are going to be resolved in the ContractOverride are resolved
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

      PilotCastInterpolator.Instance.InterpolateContractDialogueCast();
    }
  }
}