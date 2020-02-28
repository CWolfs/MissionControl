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

      Main.LogDebug($"[SetTeamByTagResult] Found'{combatants.Count}' combatants");
      Team newTeam = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(TeamUtils.GetTeamGuid(Team));
      Lance newLance = new Lance(newTeam);
      newLance.team = newTeam;

      foreach (ICombatant combatant in combatants) {
        if (combatant is AbstractActor) {
          AbstractActor actor = combatant as AbstractActor;

          actor.lance.RemoveUnitGUID(actor.GUID);
          actor.RemoveFromLance();
          actor.team.RemoveUnit(actor);

          actor.AddToTeam(newTeam);
          newLance.AddUnitGUID(actor.GUID);
          actor.AddToLance(newLance);
          newLance.team.AddUnit(actor);

          actor.team.lances.Add(newLance);
        } else {
          combatant.RemoveFromTeam();
          combatant.AddToTeam(newTeam);
        }

        if (ApplyTags != null) {
          combatant.EncounterTags.AddRange(ApplyTags);
        }

        CombatantSwitchedTeams message = new CombatantSwitchedTeams(combatant.GUID, newTeam.GUID);
        this.combat.MessageCenter.PublishMessage(message);

        LazySingletonBehavior<FogOfWarView>.Instance.FowSystem.Rebuild();
      }

      if (newLance.unitGuids.Count > 0) {
        newTeam.lances.Add(newLance);
        UnityGameInstance.BattleTechGame.Combat.ItemRegistry.AddItem(newLance);

        if (this.AlertLance) {
          newLance.BroadcastAlert();
        }
      }
    }
  }
}
