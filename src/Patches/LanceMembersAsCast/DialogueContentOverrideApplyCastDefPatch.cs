using Harmony;

using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(DialogueContentOverride), "ApplyCastDef")]
  public class DialogueContentOverrideApplyCastDefPatch {
    public static void Postfix(Contract contract, ref string ___selectedCastDefId) {
      DialogueApplyCastDefCommon.HandlePilotCast(contract, ref ___selectedCastDefId);
    }
  }
}