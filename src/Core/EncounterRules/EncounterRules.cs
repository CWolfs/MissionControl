using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace SpawnVariation.Rules {
  public abstract class EncounterRules {
    public EncounterRules() { }

    public abstract void UpdateSpawns();
  }
}