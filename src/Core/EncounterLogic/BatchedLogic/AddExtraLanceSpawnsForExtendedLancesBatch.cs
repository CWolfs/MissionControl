using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;
using MissionControl.Trigger;

namespace MissionControl.Logic {
  public class AddExtraLanceSpawnsForExtendedLancesBatch {
    public LogicState state = new LogicState();

    public AddExtraLanceSpawnsForExtendedLancesBatch(EncounterRules encounterRules) {
      encounterRules.EncounterLogic.Add(new AddExtraLanceMembers(state));
      encounterRules.EncounterLogic.Add(new AddExtraLanceMembersIndividualSecondPass(state));
      encounterRules.EncounterLogic.Add(new AddExtraLanceSpawnPoints(encounterRules, state));
      encounterRules.EncounterLogic.Add(new SpawnObjectsAroundTarget(encounterRules, state, SceneManipulationLogic.LookDirection.AWAY_FROM_TARGET));
    }
  }
}