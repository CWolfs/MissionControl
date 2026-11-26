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
      if (spawnerGameLogic == null) {
        Main.Logger.LogError($"[SetTeamByLanceSpawnerGuid] Could not find LanceSpawnerGameLogic with Guid '{LanceSpawnerGuid}'.");
        return;
      }

      Lance lance = spawnerGameLogic.GetLance();
      if (lance == null) {
        Main.Logger.LogError($"[SetTeamByLanceSpawnerGuid] LanceSpawnerGameLogic '{LanceSpawnerGuid}' has no spawned Lance.");
        return;
      }

      List<AbstractActor> lanceUnits = lance.GetLanceUnits();

      Main.LogDebug($"[SetTeamByLanceSpawnerGuid] Found '{lanceUnits.Count}' lance units");
      string newTeamGuid = TeamUtils.GetTeamGuid(Team);
      Team oldTeam = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(spawnerGameLogic.teamDefinitionGuid);
      Team newTeam = string.IsNullOrEmpty(newTeamGuid) ? null : UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<Team>(newTeamGuid);
      if (oldTeam == null || newTeam == null) {
        Main.Logger.LogError($"[SetTeamByLanceSpawnerGuid] Could not resolve teams (oldTeam='{spawnerGameLogic.teamDefinitionGuid}', newTeam='{Team}').");
        return;
      }

      // MC-711 diagnostic: capture turn-system context on every team swap so an intermittent soft-lock
      // can be diagnosed against the preceding game state without needing to reproduce.
      TurnDirector turnDirector = UnityGameInstance.BattleTechGame.Combat.TurnDirector;
      Main.LogDebug($"[SetTeamByLanceSpawnerGuid][MC-711] round={turnDirector.CurrentRound} phase={turnDirector.CurrentPhase} activeTurnActorGuid={turnDirector.ActiveTurnActor?.GUID ?? "<null>"}");
      Main.LogDebug($"[SetTeamByLanceSpawnerGuid][MC-711] oldTeam name='{oldTeam.Name}' guid={oldTeam.GUID} isActive={oldTeam.IsActive} activationSequenceSet={oldTeam.ActivationSequence != null} unitsCount={oldTeam.units.Count}");
      Main.LogDebug($"[SetTeamByLanceSpawnerGuid][MC-711] newTeam name='{newTeam.Name}' guid={newTeam.GUID} isActive={newTeam.IsActive} activationSequenceSet={newTeam.ActivationSequence != null} unitsCount={newTeam.units.Count}");

      if (oldTeam == newTeam) {
        if (ApplyTags != null) {
          foreach (AbstractActor actor in lanceUnits) {
            actor.EncounterTags.AddRange(ApplyTags);
          }
        }

        if (this.AlertLance) {
          lance.BroadcastAlert();
        }
        return;
      }

      spawnerGameLogic.teamDefinitionGuid = newTeamGuid;
      if (spawnerGameLogic.unitSpawnPointGameLogicList != null) {
        foreach (UnitSpawnPointGameLogic spawnPoint in spawnerGameLogic.unitSpawnPointGameLogicList) {
          spawnPoint?.SetTeamDefinitionGuid(newTeamGuid);
        }
      }
      spawnerGameLogic.encounterTags.Remove(oldTeam.Name);
      spawnerGameLogic.encounterTags.Add(newTeam.Name);

      oldTeam.lances.Remove(lance);
      lance.team = newTeam;
      if (lance.VisibilityCache != null) {
        lance.VisibilityCache.TeamGuid = newTeam.GUID;
        lance.VisibilityCache.ParentCache = newTeam.VisibilityCache;
      }
      newTeam.lances.Add(lance);

      GameObject hudGo = GameObject.Find("uixPrfPanl_HUD(Clone)");
      CombatHUDInWorldElementMgr inworldElementManager = hudGo?.GetComponent<CombatHUDInWorldElementMgr>();

      foreach (AbstractActor actor in lanceUnits) {
        Main.LogDebug($"[SetTeamByLanceSpawnerGuid][MC-711] actor {actor.LogDisplayName} currentTeam='{actor.team?.Name}' isInterruptActor={actor.IsInterruptActor} hasBegunActivation={actor.HasBegunActivation} hasActivatedThisRound={actor.HasActivatedThisRound} initiative={actor.Initiative}");

        actor.EncounterTags.Remove(oldTeam.Name);
        actor.EncounterTags.Add(newTeam.Name);

        if (ApplyTags != null) {
          actor.EncounterTags.AddRange(ApplyTags);
        }

        if (actor.IsDead) {
          continue;
        }

        if (actor.VisibilityCache != null) {
          oldTeam.VisibilityCache?.RemoveChildCache(actor.VisibilityCache);
        }
        oldTeam.RemoveUnit(actor);
        newTeam.AddUnit(actor);

        if (inworldElementManager != null) {
          if (oldTeam.GUID == TeamUtils.NEUTRAL_TO_ALL_TEAM_ID) {
            AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "AddInWorldActorElements").Invoke(inworldElementManager, new object[] { actor });
          } else if (newTeam.GUID == TeamUtils.NEUTRAL_TO_ALL_TEAM_ID) {
            AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "RemoveInWorldUI").Invoke(inworldElementManager, new object[] { actor.GUID });
          }
        }

        CombatantSwitchedTeams message = new CombatantSwitchedTeams(actor.GUID, newTeam.GUID);
        this.combat.MessageCenter.PublishMessage(message);
      }

      oldTeam.VisibilityCache?.RebuildCache(UnityGameInstance.BattleTechGame.Combat.GetAllLivingCombatants());
      LazySingletonBehavior<FogOfWarView>.Instance.FowSystem.Rebuild();

      if (this.AlertLance) {
        lance.BroadcastAlert();
      }
    }
  }
}
