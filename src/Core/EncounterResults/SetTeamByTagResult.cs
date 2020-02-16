using BattleTech;
using BattleTech.Framework;

using HBS.Collections;

using System;
using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetTeamByTagResult : EncounterResult {
    public string Team { get; set; }
    public string[] Tags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[SetTeamByTagResult] Setting Team '{Team}' with tags '{String.Concat(Tags)}'");
      List<ICombatant> combatants = ObjectiveGameLogic.GetTaggedCombatants(UnityGameInstance.BattleTechGame.Combat, new TagSet(Tags));

      foreach (ICombatant combatant in combatants) {
        combatant.RemoveFromTeam();

        Team newTeam = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(TeamUtils.GetTeamGuid(Team));
        combatant.AddToTeam(newTeam);
      }
    }
  }
}
