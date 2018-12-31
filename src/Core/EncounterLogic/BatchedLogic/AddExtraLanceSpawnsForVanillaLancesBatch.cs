using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;
using MissionControl.Trigger;

namespace MissionControl.Logic {
  public class AddExtraLanceSpawnsForVanillaLancesBatch {
    public AddExtraLanceSpawnsForVanillaLancesBatch(EncounterRules encounterRules) {
      encounterRules.EncounterLogic.Add(new AddExtraLanceSpawnPoints());
    }
  }
}