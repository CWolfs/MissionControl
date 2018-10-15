using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Trigger;
using MissionControl.Config;
using MissionControl.Logic;

namespace MissionControl.Rules {
  public class DestroyBaseJointAssaultEncounterRules : EncounterRules {
    private GameObject PlotBase { get; set; }

    public DestroyBaseJointAssaultEncounterRules() : base() { }

    public override void Build() {
      Main.Logger.Log("[DestroyBaseJointAssaultEncounterRules] Setting up rule object references");
      BuildAi();
      BuildSpawn();
      BuildAdditionalLances("PlotBase", SpawnLogic.LookDirection.AWAY_FROM_TARGET, "SpawnerPlayerLance", SpawnLogic.LookDirection.AWAY_FROM_TARGET, 150f, 250f);
    }

    public void BuildAi() {
      EncounterLogic.Add(new IssueFollowLanceOrderTrigger(new List<string>(){ "Employer" }, IssueAIOrderTo.ToLance, new List<string>() { "Player 1" }));
    }

    private void BuildSpawn() {
      Main.Logger.Log("[DestroyBaseJointAssaultEncounterRules] Building player spawn rule");
      EncounterLogic.Add(new SpawnLanceAtEdgeOfBoundary(this, "SpawnerPlayerLance", "PlotBase"));
    }

    public override void LinkObjectReferences(string mapName) {
      ObjectLookup.Add("PlotBase", GameObject.Find(GetPlotBaseName(mapName)));
    }
  }
}