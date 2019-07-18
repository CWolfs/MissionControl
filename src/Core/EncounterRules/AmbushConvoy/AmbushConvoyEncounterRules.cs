using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Logic;
using MissionControl.Data;

namespace MissionControl.Rules {
  public class AmbushConvoyEncounterRules : EncounterRules {
    public AmbushConvoyEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[AmbushConvoyEncounterRules] Setting up rule object references");
      BuildAi();
      BuildSpawns();
      BuildAdditionalLances("ConvoyUnit1Spawn", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>(){ Tags.EMPLOYER_TEAM }, IssueAIOrderTo.ToLance, new List<string>() { Tags.PLAYER_1_TEAM }));
    }

    public void BuildSpawns() {
      Main.Logger.Log("[AmbushConvoyEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "ConvoyUnit1Spawn"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("ConvoyUnit1Spawn", EncounterLayerData.gameObject.FindRecursive("UnitSpawnPoint1"));
    }
  }
}