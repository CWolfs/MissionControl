using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using BattleTech;
using BattleTech.Framework;

using HBS.Collections;

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
    private static Regex preGroupPattern = new Regex("\\{{MC\\..*?\\}}");
    private static Regex groupSeperator = new Regex("(\\{MC\\..*?\\})\\s?(AND|OR)?\\s?");
    private static Regex postMessagePattern = new Regex("\\[MC\\..*?\\]");
    private static Regex postGroupPattern = new Regex("\\[[MC\\..*?\\]]");

    public string Interpolate(InterpolateType interpolateType, string message, CastDef speaker = null) {
      Regex messagePattern = interpolateType == InterpolateType.PreInterpolate ? preMessagePattern : postMessagePattern;
      Regex groupPattern = interpolateType == InterpolateType.PreInterpolate ? preGroupPattern : postGroupPattern;

      // Match and process groups first in pre-interpolate only
      // All groups are conditional groups
      if (interpolateType == InterpolateType.PreInterpolate) {
        message = InterpolateGroups(interpolateType, message, speaker);

        // No need to process individual API calls if the conditional group returns a skip
        if (message == DialogueInterpolationConstants.SKIP_DIALOGUE) return message;
      }

      // Match and process individual matches
      message = InterpolateIndividuals(interpolateType, messagePattern, message, speaker);

      return message;
    }

    /*
      For each API group:
        - Find the two API calls and operator
        - Run each API call and store the result
        - Check both results agaisnt the operator for the final result
        - Feed final result back into the returned result
    */
    private string InterpolateGroups(InterpolateType interpolateType, string message, CastDef speaker) {
      MatchCollection groupMatchCollection = preGroupPattern.Matches(message);
      if (groupMatchCollection.Count > 0) {
        Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] Found '{groupMatchCollection.Count}' MC interpolation GROUP matches");
      }

      for (int i = 0; i < groupMatchCollection.Count; i++) {
        List<string> conditionals = new List<string>();
        List<string> conditionalResults = new List<string>();
        string conditionalOperator = DialogueInterpolationConstants.ConditionalOperatorAND;

        Match match = groupMatchCollection[i];
        string matchValue = match.Value;
        int index = match.Index;
        int length = match.Length;
        string resolvedData = "ERROR";
        // Main.LogDebug("[Group Test] First match of multi-conditional is: " + matchValue);

        Match m = groupSeperator.Match(matchValue);
        // int matchCount = 0;
        while (m.Success) {
          // Main.LogDebug("Match" + (++matchCount));
          for (int j = 1; j <= 2; j++) {
            Group g = m.Groups[j];
            // Main.LogDebug("Group" + j + "='" + g + "'");

            switch (j) {
              case 1: conditionals.Add(g.ToString()); break;
              case 2: {
                if (g.ToString() != null && g.ToString() != "") {
                  conditionalOperator = g.ToString();
                }
                break;
              }
            }
          }

          m = m.NextMatch();
        }

        // Execute the conditionals
        foreach (string conditional in conditionals) {
          string result = InterpolateIndividuals(interpolateType, preMessagePattern, conditional, speaker);
          conditionalResults.Add(result);
        }

        // Check conditional results against operator
        if (conditionalOperator == DialogueInterpolationConstants.ConditionalOperatorAND) {
          if (conditionalResults.All((result) => result == "")) {
            resolvedData = "";
          } else {
            resolvedData = DialogueInterpolationConstants.SKIP_DIALOGUE;
          }
        } else if (conditionalOperator == DialogueInterpolationConstants.ConditionalOperatorOR) {
          if (conditionalResults.Any((result) => result == "")) {
            resolvedData = "";
          } else {
            resolvedData = DialogueInterpolationConstants.SKIP_DIALOGUE;
          }
        } else {
          Main.Logger.LogError($"[DialogueInterpolator] Invalid conditional operator used of '{conditionalOperator}'.");
        }

        if (resolvedData == DialogueInterpolationConstants.SKIP_DIALOGUE) {
          message = DialogueInterpolationConstants.SKIP_DIALOGUE;
        } else {
          message = message.Remove(index, length).Insert(index, resolvedData);
        }
      }

      return message;
    }

    private string InterpolateIndividuals(InterpolateType interpolateType, Regex messagePattern, string message, CastDef speaker) {
      MatchCollection matchCollection = messagePattern.Matches(message);
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
          resolvedData = PostInterpolate(speaker, message, lookups);
        }

        if (resolvedData == DialogueInterpolationConstants.SKIP_DIALOGUE) {
          message = DialogueInterpolationConstants.SKIP_DIALOGUE;
        } else {
          message = message.Remove(index, length).Insert(index, resolvedData);
        }

        matchCollection = messagePattern.Matches(message);
      }

      return message;
    }

    public string PreInterpolate(CastDef speakerCastDef, string message, string[] lookups) {
      switch (lookups[1]) {
        case DialogueInterpolationConstants.PlayerLances: return InterpolatePlayerLances(InterpolateType.PreInterpolate, speakerCastDef, message, lookups);
        case DialogueInterpolationConstants.Modification: return InterpolateModification(InterpolateType.PreInterpolate, speakerCastDef, message, lookups);
        case DialogueInterpolationConstants.Conditional: return InterpolateConditional(InterpolateType.PreInterpolate, speakerCastDef, message, lookups);
        default: break;
      }
      return "MC_INCORRECT_PREINTERPOLATE_COMMAND";
    }

    public string PostInterpolate(CastDef speakerCastDef, string message, string[] lookups) {
      switch (lookups[1]) {
        case DialogueInterpolationConstants.Format: return InterpolateFormat(InterpolateType.PostInterpolate, message, lookups);
        case DialogueInterpolationConstants.Modification: return InterpolateModification(InterpolateType.PreInterpolate, speakerCastDef, message, lookups);
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

    private string InterpolateModification(InterpolateType interpolateType, CastDef speakerCastDef, string message, string[] lookups) {
      Main.LogDebug($"[Interpolate.{interpolateType.ToString()}] Modification interpolation");
      string fallbackData = "MC_INCORRECT_MODIFICATION_COMMAND";
      string modificationType = lookups[2];
      string modificationSubject = lookups[3];
      string modificationValue = lookups[4];
      string lowercaseValue = modificationValue.ToLower();

      TagSet tags = null;
      if (modificationSubject == "EncounterTags") {
        tags = MissionControl.Instance.EncounterTags;
      } else if (modificationSubject == "CompanyTags") {
        tags = UnityGameInstance.Instance.Game.Simulation.CompanyTags;
      } else if (modificationSubject == "CommanderTags") {
        tags = UnityGameInstance.Instance.Game.Simulation.CommanderTags;
      } else if (modificationSubject == "SystemTags") {
        tags = UnityGameInstance.Instance.Game.Simulation.CurSystem.Tags;
      }

      if (modificationType == DialogueInterpolationConstants.ModificationAddTo) {
        tags.Add(lowercaseValue);
        return "";
      } else if (modificationType == DialogueInterpolationConstants.ModificationRemoveFrom) {
        tags.Remove(lowercaseValue);
        return "";
      } else {
        Main.Logger.LogError($"[InterpolateModification] Unknown modification type of '{modificationType}'");
      }

      return fallbackData;
    }

    private string InterpolateConditional(InterpolateType interpolateType, CastDef speakerCastDef, string message, string[] lookups) {
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
      } else if (conditionalSubject == "PlayerLances") {
        return InterpolatePlayerLancesConditional(speakerCastDef, lookups);
      } else if (conditionalSubject == "Company") {
        return InterpolateCompanyConditional(lookups);
      } else if (conditionalSubject == "Encounter") {
        return InterpolateEncounterConditional(lookups);
      } else if (conditionalSubject == "System") {
        return InterpolateSystemConditional(lookups);
      } else if (conditionalSubject == "RandomPercentage") {
        int testValue = 0;
        string conditionalValueWithoutSymbol = conditionalValue.Replace("%", "");

        if (!int.TryParse(conditionalValueWithoutSymbol, out testValue)) {
          Main.Logger.LogError($"[InterpolateConditional.RandomPercentage] Provided value of '{testValue}' is not an integar. It must be for this check.");
        }

        int randomPercentage = UnityEngine.Random.Range(0, 101);
        Main.LogDebug($"[InterpolateConditional.RandomPercentage] Rolled a '{randomPercentage}'");

        if (conditionalType == DialogueInterpolationConstants.ConditionalTypePositive) {
          if (randomPercentage == testValue) return "";
        } else if (conditionalType == DialogueInterpolationConstants.ConditionalTypeIsLessThan) {
          if (randomPercentage < testValue) return "";
        } else if (conditionalType == DialogueInterpolationConstants.ConditionalTypeIsLessThanOrIs) {
          if (randomPercentage <= testValue) return "";
        } else if (conditionalType == DialogueInterpolationConstants.ConditionalTypeIsGreaterThan) {
          if (randomPercentage > testValue) return "";
        } else if (conditionalType == DialogueInterpolationConstants.ConditionalTypeIsGreaterThanOrIs) {
          if (randomPercentage >= testValue) return "";
        }

        return DialogueInterpolationConstants.SKIP_DIALOGUE;
      }

      return fallbackData;
    }

    private string InterpolatePlayerLancesConditional(CastDef speakerCastDef, string[] lookups) {
      string conditionalTarget = lookups[3];        // TeamPilot_Random_1 | Speaker | etc
      string conditionalType = lookups[4];          // HasTag | HasNoTag | HasAllTags | HasAnyTag
      string conditionalValue = lookups[5];         // Tag Name

      AbstractActor unit = GetBoundUnit(conditionalTarget);

      // Continue with interpolation
      if (conditionalTarget.StartsWith(DialogueInterpolationConstants.TeamPilot_Random)) {
        if (unit != null) {
          PilotDef pilotDef = unit.GetPilot().pilotDef;
          if (IsTagConditional(conditionalType)) {
            return InterpolateTagsConditional(pilotDef.PilotTags, conditionalType, conditionalValue);
          }
        }
      } else if (conditionalTarget == DialogueInterpolationConstants.Commander) {
        bool commanderIsDead = unit != null && unit.IsDead;
        if (commanderIsDead) return DialogueInterpolationConstants.SKIP_DIALOGUE;

        if (IsTagConditional(conditionalType)) {
          TagSet commanderTags = UnityGameInstance.Instance.Game.Simulation.CommanderTags;
          return InterpolateTagsConditional(commanderTags, conditionalType, conditionalValue);
        }
      } else if (conditionalTarget == DialogueInterpolationConstants.Speaker) { // Interact with the actor/pilot talking
        unit = GetSpeakerUnit(RuntimeCastFactory.GetPilotDefIDFromCastDefID(speakerCastDef.id));

        if (unit != null) {
          if (IsTagConditional(conditionalType)) {
            TagSet pilotTags = unit.GetPilot().pilotDef.PilotTags;
            return InterpolateTagsConditional(pilotTags, conditionalType, conditionalValue);
          }
        }
      }

      return DialogueInterpolationConstants.SKIP_DIALOGUE;
    }

    private string InterpolateCompanyConditional(string[] lookups) {
      string conditionalType = lookups[3];          // HasTag | HasNoTag | HasAllTags | HasAnyTag
      string conditionalValue = lookups[4];         // Tag Name

      if (IsTagConditional(conditionalType)) {
        TagSet tags = UnityGameInstance.Instance.Game.Simulation.CompanyTags;
        return InterpolateTagsConditional(tags, conditionalType, conditionalValue);
      }

      return DialogueInterpolationConstants.SKIP_DIALOGUE;
    }

    private string InterpolateEncounterConditional(string[] lookups) {
      string conditionalType = lookups[3];          // HasTag | HasNoTag | HasAllTags | HasAnyTag
      string conditionalValue = lookups[4];         // Tag Name

      if (IsTagConditional(conditionalType)) {
        TagSet tags = MissionControl.Instance.EncounterTags;
        return InterpolateTagsConditional(tags, conditionalType, conditionalValue);
      }

      return DialogueInterpolationConstants.SKIP_DIALOGUE;
    }

    private string InterpolateSystemConditional(string[] lookups) {
      string conditionalType = lookups[3];          // HasTag | HasNoTag | HasAllTags | HasAnyTag
      string conditionalValue = lookups[4];         // Tag Name

      if (IsTagConditional(conditionalType)) {
        TagSet tags = UnityGameInstance.Instance.Game.Simulation.CurSystem.Tags;
        return InterpolateTagsConditional(tags, conditionalType, conditionalValue);
      }

      return DialogueInterpolationConstants.SKIP_DIALOGUE;
    }

    private bool IsTagConditional(string conditionalType) {
      return conditionalType == "HasTag" || conditionalType == "HasNoTag" || conditionalType == "HasAllTags" || conditionalType == "HasAnyTag" || conditionalType == "HasNoTags";
    }

    private string InterpolateTagsConditional(TagSet existingTags, string conditionalType, string tagValues) {
      Main.LogDebug("[InterpolateTagsConditional] Current tags in tagset are: " + existingTags.ToJSON() + " conditionalType: " + conditionalType + " tagValues " + tagValues);
      string[] tags = tagValues.Split('|');

      if (conditionalType == "HasTag") {
        if (existingTags.Contains(tags[0])) return "";
      } else if (conditionalType == "HasNoTag") {
        if (!existingTags.Contains(tags[0])) return "";
      } else if (conditionalType == "HasAllTags") {
        if (existingTags.ContainsAll(new TagSet(tags))) return "";
      } else if (conditionalType == "HasAnyTag") {
        if (existingTags.ContainsAny(new TagSet(tags))) return "";
      } else if (conditionalType == "HasNoTags") {
        if (!existingTags.ContainsAny(new TagSet(tags))) return "";
      }

      return DialogueInterpolationConstants.SKIP_DIALOGUE;
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