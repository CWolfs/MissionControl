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
  public class LookAtTarget : SceneManipulationLogic {
    private string focusKey = "";
    private string orientationTargetKey = "";
    private GameObject focus;
    private GameObject orientationTarget;
    private bool isLance = true;

    public LookAtTarget(EncounterRules encounterRules, string focusKey, string orientationTargetKey, bool isLance) : base(encounterRules) {
      this.focusKey = focusKey;
      this.orientationTargetKey = orientationTargetKey;
      this.isLance = isLance;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[LookAtTarget] For {focus.name} to look at {orientationTarget.name}");

      if (isLance) SaveSpawnPositions(focus);
      RotateToTarget(focus, orientationTarget);
      if (isLance) RestoreLanceMemberSpawnPositions(focus);
    }

    protected override void GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(focusKey, out focus);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);

      if (focus == null || orientationTarget == null) {
        Main.Logger.LogError("[LookAtTarget] Object references are null");
      }
    }
  }
}