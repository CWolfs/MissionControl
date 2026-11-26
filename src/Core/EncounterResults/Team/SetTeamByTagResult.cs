using BattleTech;
using BattleTech.Framework;

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
      Main.LogDebug($"[SetTeamByTagResult] Setting Team '{Team}' with tags '{String.Concat(Tags)}'");
      List<ICombatant> combatants = ObjectiveGameLogic.GetTaggedCombatants(UnityGameInstance.BattleTechGame.Combat, new TagSet(Tags));

      Main.LogDebug($"[SetTeamByTagResult] Found '{combatants.Count}' combatants");
      string newTeamGuid = TeamUtils.GetTeamGuid(Team);
      Team newTeam = string.IsNullOrEmpty(newTeamGuid) ? null : UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(newTeamGuid);
      if (newTeam == null) {
        Main.Logger.LogError($"[SetTeamByTagResult] Could not find Team '{Team}'.");
        return;
      }

      foreach (ICombatant combatant in combatants) {
        if (combatant is AbstractActor) {
          /*
          AbstractActor actor = combatant as AbstractActor;

          actor.lance.RemoveUnitGUID(actor.GUID);
          actor.RemoveFromLance();
          actor.team.RemoveUnit(actor);

          actor.AddToTeam(newTeam);
          newLance.AddUnitGUID(actor.GUID);
          actor.AddToLance(newLance);
          newLance.team.AddUnit(actor);

          actor.team.lances.Add(newLance);
          */
          Main.Logger.LogError($"[SetTeamByTagResult] Using this result with AbstractActors isn't supported yet");
          continue;
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
        }

        if (ApplyTags != null) {
          combatant.EncounterTags.AddRange(ApplyTags);
        }

        CombatantSwitchedTeams message = new CombatantSwitchedTeams(combatant.GUID, newTeam.GUID);
        this.combat.MessageCenter.PublishMessage(message);

        LazySingletonBehavior<FogOfWarView>.Instance.FowSystem.Rebuild();
      }
    }
  }
}
