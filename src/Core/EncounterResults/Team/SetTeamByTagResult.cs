using UnityEngine;

using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;

using HBS;
using HBS.Collections;

using FogOfWar;

using System;
using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetTeamByTagResult : EncounterResult {
    public string Team { get; set; }
    public string[] Tags { get; set; }
    public bool AlertLance { get; set; } = true;
    public string[] ApplyTags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      if (Tags == null || Tags.Length == 0) {
        Main.Logger.LogError($"[SetTeamByTagResult] No tags specified for Team '{Team}'. Skipping.");
        return;
      }

      Main.LogDebug($"[SetTeamByTagResult] Setting Team '{Team}' with tags '{String.Concat(Tags)}'");
      CombatGameState combatState = this.combat ?? UnityGameInstance.BattleTechGame.Combat;
      List<ICombatant> combatants = ObjectiveGameLogic.GetTaggedCombatants(combatState, new TagSet(Tags));

      Main.LogDebug($"[SetTeamByTagResult] Found '{combatants.Count}' combatants");
      string newTeamGuid = TeamUtils.GetTeamGuid(Team);
      Team newTeam = string.IsNullOrEmpty(newTeamGuid) ? null : combatState.ItemRegistry.GetItemByGUID<Team>(newTeamGuid);
      if (newTeam == null) {
        Main.Logger.LogError($"[SetTeamByTagResult] Could not find Team '{Team}'.");
        return;
      }

      GameObject hudGo = GameObject.Find("uixPrfPanl_HUD(Clone)");
      CombatHUDInWorldElementMgr inworldElementManager = hudGo?.GetComponent<CombatHUDInWorldElementMgr>();

      Lance newLance = null;
      HashSet<Team> affectedOldTeams = new HashSet<Team>();
      HashSet<Lance> lancesToAlert = new HashSet<Lance>();
      bool anyTransferred = false;
      bool player1RosterChanged = false;

      foreach (ICombatant combatant in combatants) {
        if (combatant is AbstractActor actor) {
          if (actor.IsDead) {
            continue;
          }

          Team oldTeam = actor.team;

          if (oldTeam == newTeam) {
            if (ApplyTags != null) {
              actor.EncounterTags.AddRange(ApplyTags);
            }
            if (this.AlertLance && actor.lance != null) {
              lancesToAlert.Add(actor.lance);
            }
            continue;
          }

          if (oldTeam != null) {
            actor.EncounterTags.Remove(oldTeam.Name);
          }
          actor.EncounterTags.Add(newTeam.Name);

          if (ApplyTags != null) {
            actor.EncounterTags.AddRange(ApplyTags);
          }

          if (actor.lance != null) {
            actor.lance.RemoveUnitGUID(actor.GUID);
            actor.RemoveFromLance();
          }

          if (oldTeam != null) {
            if (actor.VisibilityCache != null) {
              oldTeam.VisibilityCache?.RemoveChildCache(actor.VisibilityCache);
            }
            oldTeam.RemoveUnit(actor);
            affectedOldTeams.Add(oldTeam);
          }

          if (newLance == null) {
            newLance = CreateLanceForTeam(combatState, newTeam);
            newTeam.lances.Add(newLance);
            combatState.ItemRegistry.AddItem(newLance);
          }

          newLance.AddUnitGUID(actor.GUID);
          actor.AddToLance(newLance);
          newTeam.AddUnit(actor);

          if (oldTeam != null && oldTeam.GUID == TeamUtils.PLAYER_TEAM_ID && newTeam is AITeam) {
            actor.SetBehaviorTree(BehaviorTreeIDEnum.CoreAITree);
          }

          if ((oldTeam != null && oldTeam.GUID == TeamUtils.PLAYER_TEAM_ID) || newTeam.GUID == TeamUtils.PLAYER_TEAM_ID) {
            player1RosterChanged = true;
          }

          if (inworldElementManager != null) {
            if (oldTeam != null && oldTeam.GUID == TeamUtils.NEUTRAL_TO_ALL_TEAM_ID) {
              inworldElementManager.AddInWorldActorElements(actor);
            } else if (newTeam.GUID == TeamUtils.NEUTRAL_TO_ALL_TEAM_ID) {
              inworldElementManager.RemoveInWorldUI(actor.GUID);
            }
          }

          anyTransferred = true;
          combatState.MessageCenter.PublishMessage(new CombatantSwitchedTeams(actor.GUID, newTeam.GUID));
        } else {
          if (combatant.team == newTeam && newTeam.miscCombatants != null && newTeam.miscCombatants.Contains(combatant)) {
            if (ApplyTags != null) {
              combatant.EncounterTags.AddRange(ApplyTags);
            }
            continue;
          }

          if (combatant is BattleTech.Building building && combatant.team != null) {
            combatant.team.RemoveBuilding(building);
          } else {
            combatant.team?.miscCombatants?.Remove(combatant);
            combatant.RemoveFromTeam();
          }

          newTeam.miscCombatants?.Remove(combatant);
          newTeam.AddCombatant(combatant);

          if (ApplyTags != null) {
            combatant.EncounterTags.AddRange(ApplyTags);
          }

          anyTransferred = true;
          combatState.MessageCenter.PublishMessage(new CombatantSwitchedTeams(combatant.GUID, newTeam.GUID));
        }
      }

      if (affectedOldTeams.Count > 0) {
        List<ICombatant> allLivingCombatants = combatState.GetAllLivingCombatants();
        foreach (Team oldTeam in affectedOldTeams) {
          oldTeam.VisibilityCache?.RebuildCache(allLivingCombatants);
        }
      }

      if (anyTransferred) {
        LazySingletonBehavior<FogOfWarView>.Instance.FowSystem.Rebuild();
      }

      if (player1RosterChanged) {
        combatState.MessageCenter.PublishMessage(new RefreshPortraitsMessage());
      }

      if (this.AlertLance) {
        if (newLance != null) {
          lancesToAlert.Add(newLance);
        }
        foreach (Lance lance in lancesToAlert) {
          lance.BroadcastAlert();
        }
      }
    }

    private Lance CreateLanceForTeam(CombatGameState combatState, Team team) {
      Lance lance = new Lance();
      lance.lanceGuid = Guid.NewGuid().ToString();
      lance.encounterTags = new TagSet();
      lance.encounterTags.Add(team.Name);
      lance.unitGuids = new List<string>();
      lance.BehaviorVariables = new BehaviorVariableScope();
      lance.BehaviorVariables.SetVariable(BehaviorVariableName.Bool_Alerted, new BehaviorVariableValue(b: false));
      lance.alertNotificationSubscriberGUIDs = new List<string>();
      lance.team = team;
      lance.combat = combatState;

      SharedVisibilityCache visCache = new SharedVisibilityCache(combatState, team);
      visCache.ParentCache = team.VisibilityCache;
      lance.VisibilityCache = visCache;

      return lance;
    }
  }
}
