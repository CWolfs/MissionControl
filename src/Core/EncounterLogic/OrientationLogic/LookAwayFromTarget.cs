using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.EncounterFactories;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class LookAwayFromTarget : SceneManipulationLogic {
    private string focusKey = "";
    private string orientationTargetKey = "";
    private GameObject focus;
    private GameObject orientationTarget;
    private bool isLance;

    public LookAwayFromTarget(EncounterRules encounterRules, string focusKey, string orientationTargetKey, bool isLance) : base(encounterRules) {
      this.focusKey = focusKey;
      this.orientationTargetKey = orientationTargetKey;
      this.isLance = isLance;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[LookAwayFromTarget] For {focus.name} to look away from {orientationTarget.name}");

      if (isLance) SaveSpawnPositions(focus);
      RotateAwayFromTarget(focus, orientationTarget);
      if (isLance) RestoreLanceMemberSpawnPositions(focus);      
    }

    protected override void GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(focusKey, out focus);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (focus == null || orientationTarget == null) {
        Main.Logger.LogError("[LookAwayFromTarget] Object references are null");
      }
    }
  }
}