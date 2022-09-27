using Harmony;

using System.Text.RegularExpressions;

using BattleTech;
using BattleTech.StringInterpolation;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Interpolator), "Interpolate")]
  public class InterpolatorInterpolatePatch {
    private static Regex re = new Regex("\\{MC\\..*?\\}");

    public static void Prefix(ref string template, GameContext context, bool localize) {
      if (template == null) return;

      MatchCollection matchCollection = re.Matches(template);
      if (matchCollection.Count > 0) {
        Main.LogDebug("[InterpolatorInterpolatePatch] Found '{matchCollection.Count}' MC interpolation matches");
      }

      while (matchCollection.Count > 0) {
        Match match = matchCollection[0];
        string value = match.Value;
        int index = match.Index;
        int length = match.Length;

        string[] separatedValues = value.Substring(1, value.Length - 2).Split('.');
        Main.LogDebug("[InterpolatorInterpolatePatch] MC interpolation commands " + string.Join(", ", separatedValues));

        if (separatedValues[1] == "PlayerLances") {
          Main.LogDebug("[InterpolatorInterpolatePatch] PlayerLances interpolation");
          string key = separatedValues[2];
          int bindingID = int.Parse(key.Substring(key.LastIndexOf("_") + 1));
          string resolvedData = "MC_INCORRECT_PLAYERLANCE_COMMAND";

          if (MissionControl.Instance.DynamicCastDefs.ContainsKey(bindingID)) {
            string castDefId = MissionControl.Instance.DynamicCastDefs[bindingID];
            CastDef castDef = UnityGameInstance.Instance.Game.DataManager.CastDefs.Get(castDefId);
            resolvedData = castDef.Callsign() == null ? castDef.FirstName() : castDef.Callsign();
          }

          template = template.Remove(index, length).Insert(index, resolvedData);
        }

        matchCollection = re.Matches(template);
      }
    }
  }
}