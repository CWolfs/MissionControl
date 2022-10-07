using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using BattleTech;
using BattleTech.Framework;

using MissionControl.RuntimeCast;

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

    public string Interpolate(InterpolateType interpolateType, string message, CastDef speaker = null) {
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
          resolvedData = PreInterpolate(speaker, message, lookups);
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

    public string PreInterpolate(CastDef speakerCastDef, string message, string[] lookups) {
      switch (lookups[1]) {
        case DialogueInterpolationConstants.PlayerLances: return InterpolatePlayerLances(InterpolateType.PreInterpolate, speakerCastDef, message, lookups);
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

    private string InterpolatePlayerLances(InterpolateType interpolateType, CastDef speakerCastDef, string message, string[] lookups) {
      Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] PlayerLances interpolation");
      string fallbackData = "MC_INCORRECT_PLAYERLANCE_COMMAND";

      if (lookups.Length != 4) return "MC_INCORRECT_ARGUMENTS_PROVIDED_FOR_PLAYERLANCE_COMMAND";

      string unitKey = lookups[2];
      string unitDataKey = lookups[3];

      AbstractActor unit = GetBoundUnit(unitKey);

      // Continue with interpolation
      if (unitKey.StartsWith(DialogueInterpolationConstants.TeamPilot_Random)) {
        if (unitDataKey == "DisplayName") {
          if (PilotCastInterpolator.Instance.DynamicCastDefs.ContainsKey(unitKey)) {
            string castDefId = PilotCastInterpolator.Instance.DynamicCastDefs[unitKey];
            CastDef castDef = UnityGameInstance.Instance.Game.DataManager.CastDefs.Get(castDefId);
            return castDef.Callsign() == null ? castDef.FirstName() : castDef.Callsign();
          }
        } else if (unitDataKey == "UnitName") {
          if (unit != null) {
            return unit.UnitName;
          } else {
            return "Argo";
          }
        } else if (unitDataKey == "UnitVariant") {
          if (unit != null) {
            return unit.VariantName;
          } else {
            return "Argo";
          }
        }
      } else if (unitKey == DialogueInterpolationConstants.Commander) {
        bool commanderIsDead = unit != null && unit.IsDead;
        if (commanderIsDead) unit = null;

        if (unitDataKey == "DisplayName") {
          return commanderIsDead ? "Darius" : UnityGameInstance.Instance.Game.Simulation.Commander.Name;
        } else if (unitDataKey == "UnitName") {
          if (unit != null) {
            return unit.UnitName;
          } else {
            return "Argo";
          }
        } else if (unitDataKey == "UnitVariant") {
          if (unit != null) {
            return unit.VariantName;
          } else {
            return "Argo";
          }
        }
      } else if (unitKey == DialogueInterpolationConstants.Speaker) { // Interact with the actor/pilot talking
        unit = GetSpeakerUnit(RuntimeCastFactory.GetPilotDefIDFromCastDefID(speakerCastDef.id));

        if (unitDataKey == "DisplayName") {
          return speakerCastDef.Callsign() == null ? speakerCastDef.FirstName() : speakerCastDef.Callsign();
        } else if (unitDataKey == "UnitName") {
          if (unit != null) {
            return unit.UnitName;
          } else {
            return "Argo";
          }
        } else if (unitDataKey == "UnitVariant") {
          if (unit != null) {
            return unit.VariantName;
          } else {
            return "Argo";
          }
        }
      }

      return fallbackData;
    }

    private string InterpolateConditional(InterpolateType interpolateType, string message, string[] lookups) {
      Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] Conditional interpolation");
      string fallbackData = "MC_INCORRECT_CONDITIONAL_COMMAND";
      string conditionalSubject = lookups[2];
      string conditionalType = lookups[3];
      string conditionalValue = lookups[4];

      Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] positiveNegativeConditional '{conditionalType}' conditionalSubject '{conditionalSubject}' conditionalSpecifics '{conditionalValue}'");
      bool isPositive = true;
      if (conditionalType == DialogueInterpolationConstants.ConditionalTypePositive) {
        isPositive = true;
      } else if (conditionalType == DialogueInterpolationConstants.ConditionalTypeNegative) {
        isPositive = false;
      }

      if (conditionalSubject.EndsWith("FactionType")) {
        ContractOverride contractOverride = MissionControl.Instance.CurrentContract.Override;
        FactionDef factionDef = (conditionalSubject.StartsWith("Employer")) ? contractOverride.employerTeam.FactionDef : contractOverride.targetTeam.FactionDef;
        FactionValue factionValue = (conditionalSubject.StartsWith("Employer")) ? contractOverride.employerTeam.FactionValue : contractOverride.targetTeam.FactionValue;

        if (IsFactionType(factionDef, factionValue, conditionalValue)) {
          if (isPositive) return "";
        } else {
          if (!isPositive) return "";
        }

        return DialogueInterpolationConstants.SKIP_DIALOGUE;
      } else if (conditionalSubject.EndsWith("EmployerFactionName")) {
        ContractOverride contractOverride = MissionControl.Instance.CurrentContract.Override;
        FactionDef factionDef = (conditionalSubject.StartsWith("Employer")) ? contractOverride.employerTeam.FactionDef : contractOverride.targetTeam.FactionDef;
        string factionName = factionDef.Name;

        if (conditionalType == DialogueInterpolationConstants.ConditionalTypePositive) {
          if (factionName == conditionalValue) return "";
        } else if (conditionalType == DialogueInterpolationConstants.ConditionalTypeNegative) {
          if (factionName != conditionalValue) return "";
        } else if (conditionalType == DialogueInterpolationConstants.ConditionalTypeContains) {
          if (factionName.Contains(conditionalValue)) return "";
        }

        return DialogueInterpolationConstants.SKIP_DIALOGUE;
      } else if (conditionalSubject.EndsWith("EmployerFactionShortName")) {
        ContractOverride contractOverride = MissionControl.Instance.CurrentContract.Override;
        FactionDef factionDef = (conditionalSubject.StartsWith("Employer")) ? contractOverride.employerTeam.FactionDef : contractOverride.targetTeam.FactionDef;
        string factionName = factionDef.ShortName;

        if (conditionalType == DialogueInterpolationConstants.ConditionalTypePositive) {
          if (factionName == conditionalValue) return "";
        } else if (conditionalType == DialogueInterpolationConstants.ConditionalTypeNegative) {
          if (factionName != conditionalValue) return "";
        } else if (conditionalType == DialogueInterpolationConstants.ConditionalTypeContains) {
          if (factionName.Contains(conditionalValue)) return "";
        }

        return DialogueInterpolationConstants.SKIP_DIALOGUE;
      }

      return fallbackData;
    }

    private string InterpolateFormat(InterpolateType interpolateType, string message, string[] lookups) {
      Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] Format interpolation");
      string fallbackData = "MC_INCORRECT_FORMAT_COMMAND";
      string method = lookups[2];
      string value = lookups[3];

      if (method == "ToUpperFirst") {
        return value.ToUpperFirst();
      } else if (method == "ToUpper") {
        return value.ToUpper();
      } else if (method == "ToLower") {
        return value.ToLower();
      } else if (method == "ToAlternating") {
        return string.Concat(value.ToLower().AsEnumerable().Select((c, i) => i % 2 == 0 ? c : char.ToUpper(c)));
      }

      return fallbackData;
    }

    // MANAGEMENT METHODS

    private bool FactionNameContains(FactionDef factionDef, FactionValue factionValue, string pattern) {
      if (factionDef.Name.ToLower().Contains(pattern.ToLower())) return true;
      return false;
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

    private AbstractActor GetBoundUnit(string bindKey) {
      if (bindKey.StartsWith(DialogueInterpolationConstants.TeamPilot_Random)) {
        if (PilotCastInterpolator.Instance.BoundAbstractActors.ContainsKey(bindKey)) {
          return PilotCastInterpolator.Instance.BoundAbstractActors[bindKey];
        } else {
          Main.Logger.LogError($"[DialogueInterpolator.GetBoundUnit] Attempting to get a bound unit on a TeamPilot_Random pilot '{bindKey}' but it is not bound. This is very likely an error in the contract and it's referencing a TeamPilot_Random that hasn't been set in at least one dialogue content section in the 'selectedCastDefId' property");
        }
      } else if (bindKey == DialogueInterpolationConstants.Commander) {
        if (PilotCastInterpolator.Instance.BoundAbstractActors.ContainsKey(DialogueInterpolationConstants.Commander)) {
          return PilotCastInterpolator.Instance.BoundAbstractActors[DialogueInterpolationConstants.Commander];
        }
      }

      return null;
    }

    private AbstractActor GetSpeakerUnit(string pilotDefID) {
      Team player1Team = TeamUtils.GetTeam(TeamUtils.PLAYER_TEAM_ID);
      List<Lance> lances = player1Team.lances;
      if (lances.Count <= 0) return null;

      Lance lance = lances[0];
      List<AbstractActor> units = lance.GetLanceUnits();
      foreach (AbstractActor unit in units) {
        string unitPilotDefID = unit.GetPilot().pilotDef.Description.Id.ToUpperFirst();
        if (unitPilotDefID == pilotDefID) {
          Main.LogDebug("[DialogueInterpolator.GetSpeakerUnit] Found speaker's unit!");
          return unit;
        }
      }

      return null;
    }

    public void HandleDeadActorFromDialogueContent(ref CastDef castDef) {
      if (castDef == null) Main.Logger.LogError("[DialogueInterpolator] Was provided a null or bad castDef. If a custom contract check the 'selectedCastDefId' property to ensure it's a valid castDef (often 'castDef_' has been forgotten).");

      string castDefID = castDef.id;
      string bindingKey = (castDefID.Contains(DialogueInterpolationConstants.Commander) ? DialogueInterpolationConstants.Commander : PilotCastInterpolator.Instance.GetBindIDFromCastDefID(castDefID));

      // Handle bound units
      if (bindingKey != null) {
        AbstractActor unit = DialogueInterpolator.Instance.GetBoundUnit(bindingKey);
        while (unit != null && unit.IsDead) {
          string reboundCastDefID = PilotCastInterpolator.Instance.RebindDeadUnit(bindingKey);
          Main.LogDebug($"[Interpolate.HandleDeadActorFromDialogueContent] Unit '{unit.UnitName} with pilot '{unit.GetPilot().Name}' is dead (or ejected). Rebinding all castdefs and references for unit key '{reboundCastDefID}'");
          CastDef reboundCastDef = UnityGameInstance.Instance.Game.DataManager.CastDefs.Get(reboundCastDefID);

          castDef = reboundCastDef;
          unit = GetBoundUnit(bindingKey == DialogueInterpolationConstants.Commander ? DialogueInterpolationConstants.Darius : bindingKey);
        }
      } else {  // Attempt to see if a unit exists against the castDef pilot (PureRandom units)
        AbstractActor unit = GetSpeakerUnit(RuntimeCastFactory.GetPilotDefIDFromCastDefID(castDef.id));
        while (unit != null && unit.IsDead) {
          Contract contract = MissionControl.Instance.CurrentContract;
          SpawnableUnit[] lanceConfigUnits = contract.Lances.GetLanceUnits(TeamUtils.GetTeamGuid("Player1"));
          int randomPosition = UnityEngine.Random.Range(0, lanceConfigUnits.Length);
          string pilotDefID = lanceConfigUnits[randomPosition].PilotId;

          CastDef updatedCastDef = RuntimeCastFactory.GetCastDef(RuntimeCastFactory.GetCastDefIDFromPilotDefID(pilotDefID));
          if (castDef != null) {
            castDef = updatedCastDef;
            unit = GetSpeakerUnit(pilotDefID);
          } else {
            Main.LogDebug($"[Interpolate.HandleDeadActorFromDialogueContent] Attempted to find a new CastDef but only found null. Preventing overwriting existing castdef.");
            unit = null;
          }
        }
      }
    }
  }
}