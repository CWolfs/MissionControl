using System.Linq;
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

    public enum InterpolateType { PreInterpolate, PostInterpolate }
    private static Regex preMessagePattern = new Regex("\\{MC\\..*?\\}");
    private static Regex postMessagePattern = new Regex("\\[MC\\..*?\\]");

    public string Interpolate(InterpolateType interpolateType, string message) {
      Regex pattern = interpolateType == InterpolateType.PreInterpolate ? preMessagePattern : postMessagePattern;

      MatchCollection matchCollection = pattern.Matches(message);
      if (matchCollection.Count > 0) {
        Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] Found '{matchCollection.Count}' MC interpolation matches");
      }

      while (matchCollection.Count > 0) {
        Match match = matchCollection[0];
        string value = match.Value;
        int index = match.Index;
        int length = match.Length;
        string resolvedData = "ERROR";

        string[] lookups = value.Substring(1, value.Length - 2).Split('.');
        Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] MC interpolation commands " + string.Join(", ", lookups));

        if (interpolateType == InterpolateType.PreInterpolate) {
          resolvedData = PreInterpolate(message, lookups);
        } else if (interpolateType == InterpolateType.PostInterpolate) {
          resolvedData = PostInterpolate(message, lookups);
        }

        if (resolvedData == DialogueInterpolationConstants.SKIP_DIALOGUE) {
          message = DialogueInterpolationConstants.SKIP_DIALOGUE;
        } else {
          message = message.Remove(index, length).Insert(index, resolvedData);
        }

        matchCollection = pattern.Matches(message);
      }

      return message;
    }

    public string PreInterpolate(string message, string[] lookups) {
      switch (lookups[1]) {
        case DialogueInterpolationConstants.PlayerLances: return InterpolatePlayerLances(InterpolateType.PreInterpolate, message, lookups);
        case DialogueInterpolationConstants.Conditional: return InterpolateConditional(InterpolateType.PreInterpolate, message, lookups);
        default: break;
      }
      return "MC_INCORRECT_PREINTERPOLATE_COMMAND";
    }

    public string PostInterpolate(string message, string[] lookups) {
      switch (lookups[1]) {
        case DialogueInterpolationConstants.Format: return InterpolateFormat(InterpolateType.PostInterpolate, message, lookups);
        default: break;
      }
      return "MC_INCORRECT_POSTINTERPOLATE_COMMAND";
    }

    private string InterpolatePlayerLances(InterpolateType interpolateType, string message, string[] lookups) {
      Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] PlayerLances interpolation");
      string fallbackData = "MC_INCORRECT_PLAYERLANCE_COMMAND";
      string unitKey = lookups[2];
      string unitDataKey = lookups[3];

      if (unitKey.StartsWith(DialogueInterpolationConstants.TeamPilot_Random)) {
        if (unitDataKey == "DisplayName") {
          if (PilotCastInterpolator.Instance.DynamicCastDefs.ContainsKey(unitKey)) {
            string castDefId = PilotCastInterpolator.Instance.DynamicCastDefs[unitKey];
            CastDef castDef = UnityGameInstance.Instance.Game.DataManager.CastDefs.Get(castDefId);
            return castDef.Callsign() == null ? castDef.FirstName() : castDef.Callsign();
          }
        } else if (unitDataKey == "UnitName") {
          // Need a reference to the unit
        } else {
          // Other commands like Unit's Mech etc
        }
      } else {
        // Other PlayerLance specific info like lance count etc
      }

      return fallbackData;
    }

    private string InterpolateConditional(InterpolateType interpolateType, string message, string[] lookups) {
      Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] Conditional interpolation");
      string fallbackData = "MC_INCORRECT_CONDITIONAL_COMMAND";
      string conditionalSubject = lookups[2];
      string positiveNegativeConditional = lookups[3];
      string conditionalSpecifics = lookups[4];

      Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] positiveNegativeConditional '{positiveNegativeConditional}' conditionalSubject '{conditionalSubject}' conditionalSpecifics '{conditionalSpecifics}'");
      bool isPositive = true;
      if (positiveNegativeConditional == DialogueInterpolationConstants.ConditionalPositive) {
        isPositive = true;
      } else if (positiveNegativeConditional == DialogueInterpolationConstants.ConditionalNegative) {
        isPositive = false;
      }

      if (conditionalSubject == "EmployerFactionType") {
        FactionDef factionDef = MissionControl.Instance.CurrentContract.Override.employerTeam.FactionDef;
        FactionValue factionValue = MissionControl.Instance.CurrentContract.Override.employerTeam.FactionValue;

        if (IsFactionType(factionDef, factionValue, conditionalSpecifics)) {
          if (isPositive) {
            return "";
          } else {
            return DialogueInterpolationConstants.SKIP_DIALOGUE;
          }
        } else {
          if (!isPositive) {
            return "";
          } else {
            return DialogueInterpolationConstants.SKIP_DIALOGUE;
          }
        }

      } else if (conditionalSubject == "EmployerFactionNameContains") {
        FactionDef factionDef = MissionControl.Instance.CurrentContract.Override.employerTeam.FactionDef;
        FactionValue factionValue = MissionControl.Instance.CurrentContract.Override.employerTeam.FactionValue;
      }

      return fallbackData;
    }

    private bool IsFactionType(FactionDef factionDef, FactionValue factionValue, string type) {
      switch (type) {
        case DialogueInterpolationConstants.FactionTypeGreatHouse: return factionValue.IsGreatHouse == true;
        case DialogueInterpolationConstants.FactionTypeClan: return factionValue.IsClan == true;
        case DialogueInterpolationConstants.FactionTypeMerc: return factionValue.IsMercenary == true;
        case DialogueInterpolationConstants.FactionTypePirate: return factionValue.IsPirate == true;
        case DialogueInterpolationConstants.FactionTypeRealFaction: return factionValue.IsRealFaction == true;
      }
      return false;
    }

    private bool FactionNameContains(FactionDef factionDef, FactionValue factionValue, string pattern) {
      if (factionDef.Name.ToLower().Contains(pattern.ToLower())) return true;
      return false;
    }

    private string InterpolateFormat(InterpolateType interpolateType, string message, string[] lookups) {
      Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] Format interpolation");
      string fallbackData = "MC_INCORRECT_FORMAT_COMMAND";
      string method = lookups[2];
      string value = lookups[3];

      if (method == "ToUpperFirst") {
        return value[0].ToString().ToUpper() + value.Substring(1);
      } else if (method == "ToUpper") {
        return value.ToUpper();
      } else if (method == "ToLower") {
        return value.ToLower();
      } else if (method == "ToAlternating") {
        return string.Concat(value.ToLower().AsEnumerable().Select((c, i) => i % 2 == 0 ? c : char.ToUpper(c)));
      }

      return fallbackData;
    }
  }
}