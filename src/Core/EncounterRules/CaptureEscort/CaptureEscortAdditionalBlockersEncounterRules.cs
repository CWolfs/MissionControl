using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class CaptureEscortAdditionalBlockersEncounterRules : EncounterRules {
    public CaptureEscortAdditionalBlockersEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[CaptureEscortAdditionalBlockersEncounterRules] Setting up rule object references");
      BuildAi();
      BuildSpawns();
      BuildAdditionalLances("EnemyBlockingLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }

    public void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>(){ "Employer" }, IssueAIOrderTo.ToLance, new List<string>() { "Player 1" }));
    }

    public void BuildSpawns() {
      Main.Logger.Log("[CaptureEscortAdditionalBlockersEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "EscortRegion"));
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "HunterLance", "EscortExtractionRegion", 300));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("EnemyBlockingLance", EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_BlockingForce"));
      ObjectLookup.Add("EscortRegion", EncounterLayerData.gameObject.FindRecursive("Region_Occupy"));
      ObjectLookup.Add("HunterLance", EncounterLayerData.gameObject.FindRecursive("Lance_Enemy_Hunter"));
      ObjectLookup.Add("EscortExtractionRegion", EncounterLayerData.gameObject.FindRecursive("Region_Extraction"));
    }
  }
}