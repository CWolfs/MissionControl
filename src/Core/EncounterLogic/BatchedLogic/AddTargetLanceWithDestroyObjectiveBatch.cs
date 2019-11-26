using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public class AddTargetLanceWithDestroyObjectiveBatch {
    public AddTargetLanceWithDestroyObjectiveBatch(EncounterRules encounterRules, string orientationTargetKey,
      SpawnLogic.LookDirection lookDirection, float minDistance, float maxDistance, string objectiveName, int priority, bool isPrimaryObjective) {

      int numberOfUnitsInLance = 4;
      string lanceGuid = Guid.NewGuid().ToString();
      string contractObjectiveGuid = Guid.NewGuid().ToString();
      string objectiveGuid = Guid.NewGuid().ToString();
      List<string> unitGuids = encounterRules.GenerateGuids(numberOfUnitsInLance);
      string targetTeamGuid = EncounterRules.TARGET_TEAM_ID;
      string spawnerName = $"Lance_Enemy_OpposingForce_{lanceGuid}";

      // AddPartialContractObjective addPartialContractObjective = new AddPartialContractObjective(contractObjectiveGuid, false, objectiveName, objectiveName);
      // addPartialContractObjective.ObjectiveGuids.Add(objectiveGuid);
      // AddContractObjectiveToEncounter addContractObjectiveToEncounter = new AddContractObjectiveToEncounter(contractObjectiveGuid);

      encounterRules.EncounterLogic.Add(new AddLanceToTargetTeam(lanceGuid, unitGuids));
      // encounterRules.EncounterLogic.Add(addPartialContractObjective);
      // encounterRules.EncounterLogic.Add(addContractObjectiveToEncounter);
      encounterRules.EncounterLogic.Add(new AddDestroyWholeUnitChunk(encounterRules, targetTeamGuid, lanceGuid, unitGuids,
         // spawnerName, objectiveGuid, objectiveName, priority, isPrimaryObjective, contractObjectiveGuid));
         spawnerName, objectiveGuid, objectiveName, priority, isPrimaryObjective));
      encounterRules.EncounterLogic.Add(new SpawnLanceMembersAroundTarget(encounterRules, spawnerName, orientationTargetKey,
        SpawnLogic.LookDirection.AWAY_FROM_TARGET, minDistance, maxDistance));

      encounterRules.ObjectReferenceQueue.Add(spawnerName);
    }
  }
}