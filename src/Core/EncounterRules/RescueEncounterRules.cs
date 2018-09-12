using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace SpawnVariation.Rules {
  public class RescueEncounterRules : EncounterRules {
    public RescueEncounterRules() : base() { }

    public override void UpdateSpawns() {
      Main.Logger.Log("[RescueEncounterRules] Updating spawns");
    }
  }
}