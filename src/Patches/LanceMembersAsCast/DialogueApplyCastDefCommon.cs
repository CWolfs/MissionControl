using BattleTech;

using MissionControl.Interpolation;

namespace MissionControl.Patches {
  public class DialogueApplyCastDefCommon {
    public static void HandlePilotCast(Contract contract, ref string selectedCastDefId) {
      string interpolatedData = PilotCastInterpolator.Instance.Interpolate(contract, selectedCastDefId);
      if (interpolatedData != null) {
        selectedCastDefId = interpolatedData;
      }
    }
  }
}