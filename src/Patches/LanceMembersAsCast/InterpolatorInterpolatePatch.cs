using Harmony;

using BattleTech;
using BattleTech.StringInterpolation;

using MissionControl.Interpolation;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Interpolator), "Interpolate")]
  public class InterpolatorInterpolatePatch {
    public static void Prefix(ref string template, GameContext context, bool localize) {
      if (template == null) return;
      template = DialogueInterpolator.Instance.Interoplate(template);
    }
  }
}