using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class FireMissionEncounterRules : EncounterRules {
    public FireMissionEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[FireMissionEncounterRules] Setting up rule object references");
      BuildAi();
      BuildSpawns();
      // BuildAdditionalLances("LanceEnemyOpposingForce", SpawnLogic.LookDirection.AWAY_FROM_TARGET,
      //  "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>(){ Tags.EMPLOYER_TEAM }, IssueAIOrderTo.ToLance, new List<string>() { Tags.PLAYER_1_TEAM }));
    }

    public void BuildSpawns() {
      Main.Logger.Log("[FireMissionEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "Chunk_BeaconRegion_1", 400f));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("ChunkBeaconRegion1", EncounterLayerData.gameObject.FindRecursive("Chunk_BeaconRegion_1"));
    }
  }
}