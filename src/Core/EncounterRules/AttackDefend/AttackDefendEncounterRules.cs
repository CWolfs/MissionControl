using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class AttackDefendEncounterRules : EncounterRules {
    public AttackDefendEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[AttackDefendEncounterRules] Setting up rule object references");
      BuildAi();
      BuildRandomSpawns();
      BuildAdditionalLances("SpawnerLanceEnemyTurret", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerLanceFriendlyTurret", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 50f, 150f);
    }

    public void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>(){ Tags.EMPLOYER_TEAM }, IssueAIOrderTo.ToLance, new List<string>() { Tags.PLAYER_1_TEAM }));
    }

    public void BuildRandomSpawns() {
      if (!Main.Settings.RandomSpawns) return;
      
      Main.Logger.Log("[DefendBaseEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceMembersAroundTarget(this, "SpawnerPlayerLance", "SpawnerLanceFriendlyTurret", "SpawnerLanceEnemyTurret", SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 150f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["SpawnerLanceFriendlyTurret"] = EncounterLayerData.gameObject.FindRecursive("Lance_Friendly_BaseTurrets");
      ObjectLookup["SpawnerLanceEnemyTurret"] = EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_Turret");
    }
  }
}