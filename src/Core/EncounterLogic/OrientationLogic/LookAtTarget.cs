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

    public LookAtTarget(EncounterRules encounterRules, string focusKey, string orientationTargetKey) : base(encounterRules) {
      this.focusKey = focusKey;
      this.orientationTargetKey = orientationTargetKey;
    }

    public override void Run(RunPayload payload) {
      GetObjectReferences();
      Main.Logger.Log($"[LookAtTarget] For {focus.name}");
      RotateToTarget(focus, orientationTarget);
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