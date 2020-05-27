using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;
using MissionControl.Trigger;
using MissionControl.Messages;

namespace MissionControl.Logic {
  public class AddPlayer2LanceWithDestroyObjectiveBatch {
    public AddPlayer2LanceWithDestroyObjectiveBatch(EncounterRules encounterRules, string orientationTargetKey,
      SpawnLogic.LookDirection lookDirection, float minDistance, float maxDistance, string objectiveName, int priority,
      bool isPrimaryObjective, bool displayToUser, bool showObjectiveOnLanceDetected, bool excludeFromAutocomplete) {

      int numberOfUnitsInLance = 4;
      string lanceGuid = Guid.NewGuid().ToString();
      string objectiveGuid = Guid.NewGuid().ToString();
      List<string> unitGuids = encounterRules.GenerateGuids(numberOfUnitsInLance);
      string targetTeamGuid = EncounterRules.PLAYER_2_TEAM_ID;
      string spawnerName = $"Lance_Enemy_OpposingForce_{lanceGuid}";

      encounterRules.EncounterLogic.Add(new AddLanceToPlayer2Team(lanceGuid, unitGuids));
      encounterRules.EncounterLogic.Add(new AddDestroyWholeUnitChunk(encounterRules, targetTeamGuid, lanceGuid, unitGuids,
        spawnerName, objectiveGuid, objectiveName, priority, isPrimaryObjective, displayToUser));
      if (!excludeFromAutocomplete) encounterRules.EncounterLogic.Add(new AddObjectiveToAutocompleteTrigger(objectiveGuid));
      encounterRules.EncounterLogic.Add(new SpawnLanceMembersAroundTarget(encounterRules, spawnerName, orientationTargetKey,
        SpawnLogic.LookDirection.AWAY_FROM_TARGET, minDistance, maxDistance));

      if (showObjectiveOnLanceDetected) {
        encounterRules.EncounterLogic.Add(new ShowObjectiveTrigger(MessageCenterMessageType.OnLanceDetected, lanceGuid, objectiveGuid, false));
      }

      encounterRules.ObjectReferenceQueue.Add(spawnerName);
    }
  }
}