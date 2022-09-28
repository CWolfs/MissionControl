using System.Text.RegularExpressions;

using BattleTech;

namespace MissionControl.Interpolation {
  public class DialogueInterpolator {
    private static DialogueInterpolator instance;
    public static DialogueInterpolator Instance {
      get {
        if (instance == null) instance = new DialogueInterpolator();
        return instance;
      }
    }

    private static Regex re = new Regex("\\{MC\\..*?\\}");

    public string Interoplate(string message) {
      MatchCollection matchCollection = re.Matches(message);
      if (matchCollection.Count > 0) {
        Main.LogDebug("[InterpolatorInterpolatePatch] Found '{matchCollection.Count}' MC interpolation matches");
      }

      while (matchCollection.Count > 0) {
        Match match = matchCollection[0];
        string value = match.Value;
        int index = match.Index;
        int length = match.Length;
        string resolvedData = "ERROR";

        string[] lookups = value.Substring(1, value.Length - 2).Split('.');
        Main.LogDebug("[InterpolatorInterpolatePatch] MC interpolation commands " + string.Join(", ", lookups));

        if (lookups[1] == "PlayerLances") {
          resolvedData = InterpolatePlayerLances(message, lookups);
        }

        message = message.Remove(index, length).Insert(index, resolvedData);
        matchCollection = re.Matches(message);
      }

      return message;
    }

    private string InterpolatePlayerLances(string message, string[] lookups) {
      Main.LogDebug("[InterpolatorInterpolatePatch] PlayerLances interpolation");
      string resolvedData = "MC_INCORRECT_PLAYERLANCE_COMMAND";
      string unitKey = lookups[2];
      string unitDataKey = lookups[3];

      if (unitKey.StartsWith(DialogueInterpolationConstants.TeamPilot_Random)) {
        int bindingID = int.Parse(unitKey.Substring(unitKey.LastIndexOf("_") + 1));

        if (unitDataKey == "DisplayName") {
          if (MissionControl.Instance.DynamicCastDefs.ContainsKey(bindingID)) {
            string castDefId = MissionControl.Instance.DynamicCastDefs[bindingID];
            CastDef castDef = UnityGameInstance.Instance.Game.DataManager.CastDefs.Get(castDefId);
            resolvedData = castDef.Callsign() == null ? castDef.FirstName() : castDef.Callsign();
          }
        } else if (unitDataKey == "UnitName") {
          // Need a reference to the unit
        } else {
          // Other commands like Unit's Mech etc
        }
      } else {
        // Other PlayerLance specific info like lance count etc
      }

      return resolvedData;
    }
  }
}