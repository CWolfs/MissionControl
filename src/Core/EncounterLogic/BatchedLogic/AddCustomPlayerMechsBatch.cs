using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;
using MissionControl.Trigger;

namespace MissionControl.Logic {
  public class AddCustomPlayerMechsBatch {
    public AddCustomPlayerMechsBatch(EncounterRules encounterRules) {
      int numberOfUnitsInLance = 4;
      string lanceGuid = Guid.NewGuid().ToString();
      string employerGuid = EncounterRules.EMPLOYER_TEAM_ID;
      List<string> unitGuids = encounterRules.GenerateGuids(numberOfUnitsInLance);
      string spawnerName = $"Lance_Custom_PlayerLance_{lanceGuid}";

      encounterRules.EncounterLogic.Add(new AddCustomPlayerLanceExtraSpawnPoints());

      encounterRules.EncounterLogic.Add(new AddCustomPlayerLanceSpawnChunk(employerGuid, lanceGuid, unitGuids, spawnerName,
        "Spawns a custom (player or Ai) controlled player lance"));

      encounterRules.EncounterLogic.Add(new SpawnLanceAroundTarget(encounterRules, spawnerName, "SpawnerPlayerLance",
         SpawnLogic.LookDirection.AWAY_FROM_TARGET, 200f, 250f, true));

      encounterRules.ObjectReferenceQueue.Add(spawnerName);
    }
  }
}