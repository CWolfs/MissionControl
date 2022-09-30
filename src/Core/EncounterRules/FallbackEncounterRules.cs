using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class FallbackEncounterRules : EncounterRules {
    public FallbackEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[FallbackEncounterRules] Setting up rule object references");
      BuildAi();
      BuildRandomSpawns();
      BuildAdditionalLances("LanceEnemyOpposingForce", SpawnLogic.LookDirection.AWAY_FROM_TARGET,
        "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>() { Tags.EMPLOYER_TEAM, Tags.ADDITIONAL_LANCE }, IssueAIOrderTo.ToLance, new List<string>() { Tags.PLAYER_1_TEAM }));
    }

    public void BuildRandomSpawns() {
      if (!MissionControl.Instance.IsRandomSpawnsAllowed()) return;

      Main.Logger.Log("[FallbackEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "LanceEnemyOpposingForce", 400f));
    }

    public override void LinkObjectReferences(string mapName) {
      // Due to the variable nature of spawners on the map - grab any lance spawner available (always going to be one) and use that as the OpFor
      ObjectLookup["LanceEnemyOpposingForce"] = GetAnyLanceSpawnerGameObject(MissionControl.Instance.EncounterLayerGameObject);
    }
  }
}