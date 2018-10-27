using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class DebugArenaSkirmishEncounterRules : EncounterRules {
    public DebugArenaSkirmishEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[DebugArenaSkirmishEncounterRules] Setting up rule object references");
      BuildAi();
      BuildSpawns();
      BuildAdditionalLances("Player2LanceSpawner", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>(){ "Employer" }, IssueAIOrderTo.ToLance, new List<string>() { "Player 1" }));
    }

    public void BuildSpawns() {
      Main.Logger.Log("[DebugArenaSkirmishEncounterRules] Building spawns rules");

      EncounterLogic.Add(new SpawnLanceAnywhere(this, "Player2LanceSpawner", "SpawnerPlayerLance"));
      EncounterLogic.Add(new SpawnLanceAroundTarget(this, "SpawnerPlayerLance", "Player2LanceSpawner", SpawnLogic.LookDirection.TOWARDS_TARGET, 100f, 150f, true));
      EncounterLogic.Add(new LookAtTarget(this, "Player2LanceSpawner", "SpawnerPlayerLance", true));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("Player2LanceSpawner", EncounterLayerData.gameObject.FindRecursive("Player2LanceSpawner"));
    }
  }
}