using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class AddTargetLanceWithDestroyObjectiveBatch {
    public AddTargetLanceWithDestroyObjectiveBatch(EncounterRules encounterRules, string orientationTargetKey,
      SpawnLogic.LookDirection lookDirection, float minDistance, float maxDistance, string objectiveName, int priority) {

        int numberOfUnitsInLance = 4;
        string lanceGuid = Guid.NewGuid().ToString();
        List<string> unitGuids = encounterRules.GenerateGuids(numberOfUnitsInLance);
        string targetTeamGuid = EncounterRules.TARGET_TEAM_ID;
        string spawnerName = $"Lance_Enemy_OpposingForce_{lanceGuid}";

        encounterRules.EncounterLogic.Add(new AddLanceToTargetTeam(lanceGuid, unitGuids));
        encounterRules.EncounterLogic.Add(new AddDestroyWholeUnitChunk(targetTeamGuid, lanceGuid, unitGuids, 
          spawnerName, objectiveName, priority));
        encounterRules.EncounterLogic.Add(new SpawnLanceMembersAroundTarget(encounterRules, spawnerName, orientationTargetKey, 
          SpawnLogic.LookDirection.AWAY_FROM_TARGET, minDistance, maxDistance));

        encounterRules.ObjectReferenceQueue.Add(spawnerName);
    }
  }
}