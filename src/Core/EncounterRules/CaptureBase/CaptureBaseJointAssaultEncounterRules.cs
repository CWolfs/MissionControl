using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class CaptureBaseJointAssaultEncounterRules : EncounterRules {
    public CaptureBaseJointAssaultEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[CaptureBaseJointAssaultEncounterRules] Setting up rule object references");
      BuildAi();
      BuildSpawns();
      BuildAdditionalLances("PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 25f, 100f);
    }
    
    public void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>(){ Tags.EMPLOYER_TEAM }, IssueAIOrderTo.ToLance, new List<string>() { Tags.PLAYER_1_TEAM }));
    }

    public void BuildSpawns() {
      Main.Logger.Log("[CaptureBaseJointAssaultEncounterRules] Building spawns rules");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup["PlotBase"] = EncounterLayerData.gameObject.FindRecursive("Chunk_OccupyRegion_Base");
    }
  }
}