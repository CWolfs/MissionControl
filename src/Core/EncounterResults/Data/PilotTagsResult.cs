using BattleTech;
using BattleTech.Framework;

using HBS.Collections;

using System;
using System.Collections.Generic;
using System.Linq;

using MissionControl.Data;

/**
This result adds or removes a tag on one or more pilots selected by triggering event, region, encounter tags, team, or pilot definition ID.
*/
namespace MissionControl.Result {
  public class PilotTagsResult : EncounterResult {
    public PilotTargetType TargetType { get; set; } = PilotTargetType.EncounterTags;
    public EventUnitRole EventUnitRole { get; set; } = EventUnitRole.AffectedUnit;
    public string RegionGuid { get; set; } = "";
    public string Team { get; set; } = "Player1";
    public string[] TargetTags { get; set; } = Array.Empty<string>();
    public string PilotId { get; set; } = "";
    public bool AliveOnly { get; set; } = false;
    public TagOperation Operation { get; set; } = TagOperation.Add;
    public string Tag { get; set; } = "";

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      string tag = Tag?.Trim();
      if (string.IsNullOrEmpty(tag)) {
        Main.Logger.LogError("[PilotTagsResult] No tag provided. Skipping.");
        return;
      }

      if (!Enum.IsDefined(typeof(TagOperation), Operation)) {
        Main.Logger.LogError($"[PilotTagsResult] Unsupported TagOperation '{Operation}'. Skipping.");
        return;
      }

      CombatGameState combatState = this.combat ?? UnityGameInstance.BattleTechGame?.Combat;
      SimGameState simGame = UnityGameInstance.Instance?.Game?.Simulation;
      List<TagSet> processedTagSets = new List<TagSet>();
      bool validationFailed = false;
      int matchedCount = 0;

      switch (TargetType) {
        case PilotTargetType.TriggeringUnit:
          matchedCount = ApplyToTriggeringUnit(inMessage, combatState, simGame, tag, processedTagSets, out validationFailed);
          break;
        case PilotTargetType.Region:
          matchedCount = ApplyToRegion(combatState, simGame, tag, processedTagSets, out validationFailed);
          break;
        case PilotTargetType.EncounterTags:
          matchedCount = ApplyToEncounterTags(combatState, simGame, tag, processedTagSets, out validationFailed);
          break;
        case PilotTargetType.Team:
          matchedCount = ApplyToTeam(combatState, simGame, tag, processedTagSets, out validationFailed);
          break;
        case PilotTargetType.PilotId:
          matchedCount = ApplyToPilotId(combatState, simGame, tag, processedTagSets, out validationFailed);
          break;
        default:
          Main.Logger.LogError($"[PilotTagsResult] Unsupported TargetType '{TargetType}'. Skipping.");
          return;
      }

      if (!validationFailed && matchedCount == 0) {
        Main.Logger.LogWarning($"[PilotTagsResult] No matching pilots found for TargetType '{TargetType}'.");
      }
    }

    private int ApplyToTriggeringUnit(MessageCenterMessage inMessage, CombatGameState combatState, SimGameState simGame, string tag, List<TagSet> processedTagSets, out bool validationFailed) {
      validationFailed = false;

      if (inMessage == null) {
        Main.Logger.LogWarning("[PilotTagsResult] TriggeringUnit mode received a null message (e.g. from a Delay or immediate trigger). Skipping.");
        validationFailed = true;
        return 0;
      }

      if (combatState == null) {
        Main.Logger.LogError("[PilotTagsResult] Combat state is null. Skipping.");
        validationFailed = true;
        return 0;
      }

      AbstractActor actor = ResolveActorFromMessage(inMessage, combatState);
      if (actor == null) {
        Main.Logger.LogWarning($"[PilotTagsResult] Could not resolve an AbstractActor from message '{inMessage.GetType().Name}' for EventUnitRole '{EventUnitRole}'.");
        validationFailed = true;
        return 0;
      }

      Main.LogDebug($"[PilotTagsResult] Triggering for TargetType '{TargetType}' Role '{EventUnitRole}' Actor '{actor.DisplayName}' Operation '{Operation}' Tag '{tag}'");
      return ApplyToActor(actor, combatState, simGame, tag, processedTagSets) ? 1 : 0;
    }

    private AbstractActor ResolveActorFromMessage(MessageCenterMessage inMessage, CombatGameState combatState) {
      if (inMessage is RegionMessageBase regionMessage) {
        return GetActorByGuid(combatState, regionMessage.ActorGuid);
      }

      if (inMessage is PilotDamagedMessage pilotDamagedMessage) {
        return GetActorByGuid(combatState, pilotDamagedMessage.affectedObjectGuid);
      }

      if (inMessage is ActorMoveEndMessage actorMoveEndMessage) {
        return GetActorByGuid(combatState, actorMoveEndMessage.affectedObjectGuid ?? actorMoveEndMessage.actingObjectGuid);
      }

      if (inMessage is AttackCompleteMessage attackCompleteMessage && attackCompleteMessage.attackSequence != null) {
        return EventUnitRole == EventUnitRole.ActingUnit
          ? attackCompleteMessage.attackSequence.attacker
          : attackCompleteMessage.attackSequence.chosenTarget as AbstractActor;
      }

      if (inMessage is EncounterObjectMessage encounterMessage) {
        string targetGuid = EventUnitRole == EventUnitRole.ActingUnit
          ? encounterMessage.actingObjectGuid
          : encounterMessage.affectedObjectGuid;
        return GetActorByGuid(combatState, targetGuid);
      }

      return null;
    }

    private static AbstractActor GetActorByGuid(CombatGameState combatState, string guid) {
      if (string.IsNullOrEmpty(guid)) return null;
      return combatState.ItemRegistry.GetItemByGUID<AbstractActor>(guid);
    }

    private int ApplyToRegion(CombatGameState combatState, SimGameState simGame, string tag, List<TagSet> processedTagSets, out bool validationFailed) {
      validationFailed = false;

      if (string.IsNullOrEmpty(RegionGuid)) {
        Main.Logger.LogError("[PilotTagsResult] No RegionGuid provided for Region mode. Skipping.");
        validationFailed = true;
        return 0;
      }

      if (combatState == null) {
        Main.Logger.LogError("[PilotTagsResult] Combat state is null. Skipping.");
        validationFailed = true;
        return 0;
      }

      RegionGameLogic regionGameLogic = combatState.ItemRegistry.GetItemByGUID<RegionGameLogic>(RegionGuid);
      if (regionGameLogic == null) {
        Main.Logger.LogError($"[PilotTagsResult] Region not found for Guid '{RegionGuid}'.");
        validationFailed = true;
        return 0;
      }

      string requiredTeamGuid = ResolveOptionalTeamGuid();
      if (!string.IsNullOrEmpty(Team) && !string.Equals(Team, "Any", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(requiredTeamGuid)) {
        Main.Logger.LogError($"[PilotTagsResult] Unknown Team '{Team}' for Region mode. Skipping.");
        validationFailed = true;
        return 0;
      }

      EncounterLayerData layerData = combatState.EncounterLayerData ?? MissionControl.Instance.EncounterLayerData;
      Main.LogDebug($"[PilotTagsResult] Triggering for TargetType '{TargetType}' RegionGuid '{RegionGuid}' Team '{Team}' AliveOnly '{AliveOnly}' Operation '{Operation}' Tag '{tag}'");

      int matchedCount = 0;
      foreach (AbstractActor actor in combatState.AllActors) {
        if (actor == null) continue;
        if (AliveOnly && actor.IsDead) continue;
        if (!string.IsNullOrEmpty(requiredTeamGuid) && actor.team?.GUID != requiredTeamGuid) continue;

        bool isInRegion = layerData != null
          ? layerData.IsPointInRegion(actor.CurrentPosition, RegionGuid)
          : actor.IsInRegion(RegionGuid);
        if (!isInRegion) continue;

        if (ApplyToActor(actor, combatState, simGame, tag, processedTagSets)) {
          matchedCount++;
        }
      }

      return matchedCount;
    }

    private int ApplyToEncounterTags(CombatGameState combatState, SimGameState simGame, string tag, List<TagSet> processedTagSets, out bool validationFailed) {
      validationFailed = false;

      string[] validTargetTags = TargetTags?.Select(t => t?.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToArray() ?? Array.Empty<string>();
      if (validTargetTags.Length == 0) {
        Main.Logger.LogError("[PilotTagsResult] No valid target tags provided for EncounterTags mode. Skipping.");
        validationFailed = true;
        return 0;
      }

      if (combatState == null) {
        Main.Logger.LogError("[PilotTagsResult] Combat state is null. Skipping.");
        validationFailed = true;
        return 0;
      }

      Main.LogDebug($"[PilotTagsResult] Triggering for TargetType '{TargetType}' TargetTags '{string.Join(",", validTargetTags)}' AliveOnly '{AliveOnly}' Operation '{Operation}' Tag '{tag}'");
      List<ICombatant> combatants = ObjectiveGameLogic.GetTaggedCombatants(combatState, new TagSet(validTargetTags));
      Main.LogDebug($"[PilotTagsResult] Found '{combatants.Count}' tagged combatants");

      int matchedCount = 0;
      foreach (ICombatant combatant in combatants) {
        if (combatant is not AbstractActor actor) {
          Main.LogDebug($"[PilotTagsResult] Skipping non-actor combatant '{combatant?.DisplayName ?? "null"}'");
          continue;
        }

        if (AliveOnly && actor.IsDead) continue;

        if (ApplyToActor(actor, combatState, simGame, tag, processedTagSets)) {
          matchedCount++;
        }
      }

      return matchedCount;
    }

    private int ApplyToTeam(CombatGameState combatState, SimGameState simGame, string tag, List<TagSet> processedTagSets, out bool validationFailed) {
      validationFailed = false;

      if (combatState == null) {
        Main.Logger.LogError("[PilotTagsResult] Combat state is null. Skipping.");
        validationFailed = true;
        return 0;
      }

      string requiredTeamGuid = ResolveOptionalTeamGuid();
      if (!string.IsNullOrEmpty(Team) && !string.Equals(Team, "Any", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(requiredTeamGuid)) {
        Main.Logger.LogError($"[PilotTagsResult] Unknown Team '{Team}' for Team mode. Skipping.");
        validationFailed = true;
        return 0;
      }

      Main.LogDebug($"[PilotTagsResult] Triggering for TargetType '{TargetType}' Team '{Team}' AliveOnly '{AliveOnly}' Operation '{Operation}' Tag '{tag}'");

      int matchedCount = 0;
      foreach (AbstractActor actor in combatState.AllActors) {
        if (actor == null) continue;
        if (AliveOnly && actor.IsDead) continue;
        if (!string.IsNullOrEmpty(requiredTeamGuid) && actor.team?.GUID != requiredTeamGuid) continue;

        if (ApplyToActor(actor, combatState, simGame, tag, processedTagSets)) {
          matchedCount++;
        }
      }

      return matchedCount;
    }

    private int ApplyToPilotId(CombatGameState combatState, SimGameState simGame, string tag, List<TagSet> processedTagSets, out bool validationFailed) {
      validationFailed = false;

      string pilotId = PilotId?.Trim();
      if (string.IsNullOrEmpty(pilotId)) {
        Main.Logger.LogError("[PilotTagsResult] No PilotId provided for PilotId mode. Skipping.");
        validationFailed = true;
        return 0;
      }

      Main.LogDebug($"[PilotTagsResult] Triggering for TargetType '{TargetType}' PilotId '{pilotId}' Operation '{Operation}' Tag '{tag}'");

      int matchedCount = 0;
      if (combatState != null) {
        foreach (AbstractActor actor in combatState.AllActors) {
          Pilot pilot = actor?.GetPilot();
          if (pilot?.pilotDef?.PilotTags == null) continue;

          if (string.Equals(pilot.pilotDef.Description?.Id, pilotId, StringComparison.Ordinal)) {
            EnsureIsolatedCombatPilotDef(pilot);
            if (ApplyOperation(pilot.pilotDef.PilotTags, tag, processedTagSets, pilotId)) {
              matchedCount++;
            }
          }
        }
      }

      if (simGame != null) {
        matchedCount += ApplyToSimGamePilotById(simGame, pilotId, tag, processedTagSets);
      }

      return matchedCount;
    }

    private string ResolveOptionalTeamGuid() {
      string trimmedTeam = Team?.Trim();
      if (string.IsNullOrEmpty(trimmedTeam) || string.Equals(trimmedTeam, "Any", StringComparison.OrdinalIgnoreCase)) {
        return null;
      }

      return TeamUtils.GetTeamGuid(trimmedTeam);
    }

    private bool ApplyToActor(AbstractActor actor, CombatGameState combatState, SimGameState simGame, string tag, List<TagSet> processedTagSets) {
      Pilot pilot = actor.GetPilot();
      if (pilot?.pilotDef?.PilotTags == null) {
        Main.Logger.LogWarning($"[PilotTagsResult] Skipping actor '{actor.DisplayName}' with no pilot or pilot tags.");
        return false;
      }

      // Capture commander eligibility before mutating tags because Pilot.IsPlayerCharacter checks PilotTags ("commander_player").
      string pilotId = pilot.pilotDef.Description?.Id;
      bool shouldMirrorCommander = simGame?.Commander?.pilotDef?.PilotTags != null &&
          actor.team == combatState.LocalPlayerTeam &&
          pilot.IsPlayerCharacter &&
          string.Equals(pilotId, simGame.Commander.pilotDef.Description?.Id, StringComparison.Ordinal);

      EnsureIsolatedCombatPilotDef(pilot);
      bool matched = ApplyOperation(pilot.pilotDef.PilotTags, tag, processedTagSets, pilotId ?? actor.DisplayName);

      // Deployed player pilots automatically persist their combat PilotTags back to SimGameState via
      // Contract.CompleteContract -> Pilot.ToPilotDef -> SimGameState.ResolveCompleteContract.
      // Only mirror immediately onto the unique SimGame Commander when the actor is the player commander,
      // so mid-combat EvaluateTagConditional(Scope: Commander) checks stay consistent without risking
      // writes to unrelated roster pilots sharing a definition ID.
      if (shouldMirrorCommander) {
        ApplyOperation(simGame.Commander.pilotDef.PilotTags, tag, processedTagSets, $"SimGame.Commander ({pilotId})");
      }

      return matched;
    }

    private static void EnsureIsolatedCombatPilotDef(Pilot pilot) {
      // Spawned NPC actors can share a cached PilotDef instance from DataManager.PilotDefs.
      // Clone the PilotDef (and its PilotTags TagSet) onto this combat Pilot without resetting live stats.
      PilotDef isolatedDef = pilot.ToPilotDef(recordPilotStats: false);
      isolatedDef.SetRecentInjuryDamageType(pilot.pilotDef.RecentInjuryDamageType);
      isolatedDef.SetRecentInjuryPilotedActorID(pilot.pilotDef.RecentInjuryPilotedActorID);
      isolatedDef.SetRecentInjurySourceActorID(pilot.pilotDef.RecentInjurySourceActorID);
      isolatedDef.SetRecentInjurySourceWeaponID(pilot.pilotDef.RecentInjurySourceWeaponID);
      pilot.pilotDef = isolatedDef;
    }

    private int ApplyToSimGamePilotById(SimGameState simGame, string pilotId, string tag, List<TagSet> processedTagSets) {
      int matchedCount = 0;

      if (simGame.Commander?.pilotDef?.PilotTags != null &&
          string.Equals(simGame.Commander.pilotDef.Description?.Id, pilotId, StringComparison.Ordinal)) {
        if (ApplyOperation(simGame.Commander.pilotDef.PilotTags, tag, processedTagSets, $"SimGame.Commander ({pilotId})")) {
          matchedCount++;
        }
      }

      if (simGame.PilotRoster != null) {
        foreach (Pilot rosterPilot in simGame.PilotRoster) {
          if (rosterPilot?.pilotDef?.PilotTags == null) continue;
          if (string.Equals(rosterPilot.pilotDef.Description?.Id, pilotId, StringComparison.Ordinal)) {
            if (ApplyOperation(rosterPilot.pilotDef.PilotTags, tag, processedTagSets, $"SimGame.PilotRoster ({pilotId})")) {
              matchedCount++;
            }
          }
        }
      }

      return matchedCount;
    }

    private bool ApplyOperation(TagSet pilotTags, string tag, List<TagSet> processedTagSets, string pilotLabel) {
      for (int i = 0; i < processedTagSets.Count; i++) {
        if (ReferenceEquals(processedTagSets[i], pilotTags)) {
          return false;
        }
      }

      processedTagSets.Add(pilotTags);

      switch (Operation) {
        case TagOperation.Add:
          if (pilotTags.Contains(tag)) {
            Main.LogDebug($"[PilotTagsResult] Pilot '{pilotLabel}' already has tag '{tag}'");
          } else {
            pilotTags.Add(tag);
            Main.LogDebug($"[PilotTagsResult] Added tag '{tag}' to pilot '{pilotLabel}'");
          }
          return true;
        case TagOperation.Remove:
          if (pilotTags.Contains(tag)) {
            pilotTags.Remove(tag);
            Main.LogDebug($"[PilotTagsResult] Removed tag '{tag}' from pilot '{pilotLabel}'");
          } else {
            Main.LogDebug($"[PilotTagsResult] Pilot '{pilotLabel}' does not have tag '{tag}'");
          }
          return true;
        default:
          Main.Logger.LogWarning($"[PilotTagsResult] Unsupported TagOperation '{Operation}'");
          return false;
      }
    }
  }
}
