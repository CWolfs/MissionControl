using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(DialogueContent), "ApplyCastDef")]
  public class DialogueContentApplyCastDefPatch {
    public static void Postfix(Contract contract, ref string ___selectedCastDefId) {
      DialogueApplyCastDefCommon.HandlePilotCast(contract, ref ___selectedCastDefId);
    }
  }
}