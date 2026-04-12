using UnityEngine;

using BattleTech;

using HBS;

using FogOfWar;

using BattleTech.UI;

using System.Collections.Generic;

using Harmony;

namespace MissionControl.Result {
  public class SetTeamByLanceSpawnerGuidResult : EncounterResult {
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

      // MC-711 diagnostic: capture turn-system context on every team swap so an intermittent soft-lock
      // can be diagnosed against the preceding game state without needing to reproduce.
      TurnDirector turnDirector = UnityGameInstance.BattleTechGame.Combat.TurnDirector;
      Main.LogDebug($"[SetTeamByLanceSpawnerGuid][MC-711] round={turnDirector.CurrentRound} phase={turnDirector.CurrentPhase} activeTurnActorGuid={turnDirector.ActiveTurnActor?.GUID ?? "<null>"}");
      Main.LogDebug($"[SetTeamByLanceSpawnerGuid][MC-711] oldTeam name='{oldTeam.Name}' guid={oldTeam.GUID} isActive={oldTeam.IsActive} activationSequenceSet={oldTeam.ActivationSequence != null} unitsCount={oldTeam.units.Count}");
      Main.LogDebug($"[SetTeamByLanceSpawnerGuid][MC-711] newTeam name='{newTeam.Name}' guid={newTeam.GUID} isActive={newTeam.IsActive} activationSequenceSet={newTeam.ActivationSequence != null} unitsCount={newTeam.units.Count}");

      spawnerGameLogic.teamDefinitionGuid = TeamUtils.GetTeamGuid(Team);
      spawnerGameLogic.encounterTags.Remove(oldTeam.Name);
      spawnerGameLogic.encounterTags.Add(newTeam.Name);

      oldTeam.lances.Remove(lance);
      newTeam.lances.Add(lance);

      foreach (AbstractActor actor in lanceUnits) {
        Main.LogDebug($"[SetTeamByLanceSpawnerGuid][MC-711] actor {actor.LogDisplayName} currentTeam='{actor.team?.Name}' isInterruptActor={actor.IsInterruptActor} hasBegunActivation={actor.HasBegunActivation} hasActivatedThisRound={actor.HasActivatedThisRound} initiative={actor.Initiative}");

        oldTeam.RemoveUnit(actor);
        actor.AddToTeam(newTeam);
        newTeam.AddUnit(actor);

        actor.EncounterTags.Remove(oldTeam.Name);
        actor.EncounterTags.Add(newTeam.Name);

        if (ApplyTags != null) {
          actor.EncounterTags.AddRange(ApplyTags);
        }

        CombatHUDInWorldElementMgr inworldElementManager = GameObject.Find("uixPrfPanl_HUD(Clone)").GetComponent<CombatHUDInWorldElementMgr>();
        if (oldTeam.GUID == TeamUtils.NEUTRAL_TO_ALL_TEAM_ID) {
          AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "AddInWorldActorElements").Invoke(inworldElementManager, new object[] { actor });
        } else if (newTeam.GUID == TeamUtils.NEUTRAL_TO_ALL_TEAM_ID) {
          AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "RemoveInWorldUI").Invoke(inworldElementManager, new object[] { actor.GUID });
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
