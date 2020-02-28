using BattleTech;
using BattleTech.Framework;

using HBS;
using HBS.Collections;

using FogOfWar;

using System;
using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetTeamByLanceSpawnerGuid : EncounterResult {
    public string Team { get; set; }
    public string LanceSpawnerGuid { get; set; }
    public bool AlertLance { get; set; } = true;
    public string[] ApplyTags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[SetTeamByLanceSpawnerGuid] Setting Team '{Team}' with Spawner Guid '{LanceSpawnerGuid}'");
      LanceSpawnerGameLogic spawnerGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<LanceSpawnerGameLogic>(LanceSpawnerGuid);
      Lance lance = spawnerGameLogic.GetLance();
      List<AbstractActor> lanceUnits = spawnerGameLogic.GetLance().GetLanceUnits();

      Main.LogDebug($"[SetTeamByLanceSpawnerGuid] Found '{lanceUnits.Count}' lance units");
      Team oldTeam = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(spawnerGameLogic.teamDefinitionGuid);
      Team newTeam = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(TeamUtils.GetTeamGuid(Team));

      spawnerGameLogic.teamDefinitionGuid = TeamUtils.GetTeamGuid(Team);
      spawnerGameLogic.encounterTags.Remove(oldTeam.Name);
      spawnerGameLogic.encounterTags.Add(newTeam.Name);

      oldTeam.lances.Remove(lance);
      newTeam.lances.Add(lance);

      foreach (AbstractActor actor in lanceUnits) {
        oldTeam.RemoveUnit(actor);
        actor.AddToTeam(newTeam);
        newTeam.AddUnit(actor);

        actor.EncounterTags.Remove(oldTeam.Name);
        actor.EncounterTags.Add(newTeam.Name);

        if (ApplyTags != null) {
          actor.EncounterTags.AddRange(ApplyTags);
        }

        CombatantSwitchedTeams message = new CombatantSwitchedTeams(actor.GUID, newTeam.GUID);
        this.combat.MessageCenter.PublishMessage(message);

        LazySingletonBehavior<FogOfWarView>.Instance.FowSystem.Rebuild();
      }

      if (this.AlertLance) {
        lance.BroadcastAlert();
      }
    }
  }
}
