using Harmony;

using BattleTech;
using BattleTech.StringInterpolation;

using MissionControl.Interpolation;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Interpolator), "Interpolate")]
  public class InterpolatorInterpolatePatch {
    public static void Postfix(ref string __result, GameContext context, bool localize) {
      if (__result == null) return;
      __result = DialogueInterpolator.Instance.Interpolate(DialogueInterpolator.InterpolateType.PostInterpolate, __result);
    }
  }
}