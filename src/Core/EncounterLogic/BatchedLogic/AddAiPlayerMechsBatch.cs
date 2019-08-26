using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;
using MissionControl.Trigger;

namespace MissionControl.Logic {
  public class AddAiPlayerMechsBatch {
    public AddAiPlayerMechsBatch(EncounterRules encounterRules) {
      int numberOfUnitsInLance = 4;
      string lanceGuid = Guid.NewGuid().ToString();
      List<string> unitGuids = encounterRules.GenerateGuids(numberOfUnitsInLance);
      string employerTeamGuid = EncounterRules.EMPLOYER_TEAM_ID;
      string spawnerName = $"Lance_Ai_PlayerForce_{lanceGuid}";

      encounterRules.EncounterLogic.Add(new AddPlayerLanceAiSpawnChunk(employerTeamGuid, lanceGuid, unitGuids, spawnerName,
        "Spawns a Ai controlled player lance"));

      encounterRules.EncounterLogic.Add(new SpawnLanceAroundTarget(encounterRules, spawnerName, "SpawnerPlayerLance",
         SpawnLogic.LookDirection.AWAY_FROM_TARGET, 50f, 150f, true));

      encounterRules.ObjectReferenceQueue.Add(spawnerName);
    }
  }
}