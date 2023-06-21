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
      FixEncounterObjectGameLogicsWithNoEncounterGUID();
      MissionControl.Instance.RunEncounterRules(LogicBlock.LogicType.ENCOUNTER_MANIPULATION);

      PilotCastInterpolator.Instance.InterpolateContractDialogueCast();
    }

    /**
    * Sometimes the maps have bad data with some EncounterObjectGameLogics having no encounterObjectGuid.
    * The game has code to fix this but it runs too late and doesn't really fix problems caused by a lack of initial encounterObjectGuid
    * MC fixes this
    */
    private static void FixEncounterObjectGameLogicsWithNoEncounterGUID() {
      Main.Logger.Log($"[FixEncounterObjectGameLogicsWithNoEncounterGUID] Checking for any EncounterObjectGameLogics that have no initial encounterObjectGuid set.");

      foreach (EncounterObjectGameLogic encounterObjectGameLogic in MissionControl.Instance.EncounterLayerData.AllEncounterObjectGameLogics) {
        if (encounterObjectGameLogic.encounterObjectGuid == "" || encounterObjectGameLogic.encounterObjectGuid == null) {
          string guid = GUIDFactory.GetGUID();
          Main.Logger.Log($"[FixEncounterObjectGameLogicsWithNoEncounterGUID] Generating new encounterObjectGuid for '{encounterObjectGameLogic.gameObject.name} in component '{encounterObjectGameLogic.GetType()}' using new GUID '{guid}'");
          encounterObjectGameLogic.encounterObjectGuid = guid;
        }
      }
    }
  }
}