using System;
using System.Collections.Generic;

using BattleTech;
using UnityEngine;

using MissionControl.Data;
using MissionControl.Conditional;
using MissionControl.Rules;
using MissionControl.Trigger;

namespace MissionControl.Logic {
  public class AddTargetLanceWithDestroyObjectiveBatch {
    public AddTargetLanceWithDestroyObjectiveBatch(EncounterRules encounterRules, string orientationTargetKey,
      SpawnLogic.LookDirection lookDirection, float mustBeBeyondDistance, float mustBeWithinDistance, string objectiveName, int priority,
      bool isPrimaryObjective, bool displayToUser, bool showObjectiveOnLanceDetected, bool excludeFromAutocomplete, List<string> lanceTags, MLanceOverride manuallySpecifiedLance = null) {

      int numberOfUnitsInLance = 4;
      string lanceGuid = Guid.NewGuid().ToString();
      string contractObjectiveGuid = Guid.NewGuid().ToString();
      string objectiveGuid = Guid.NewGuid().ToString();
      List<string> unitGuids = encounterRules.GenerateGuids(numberOfUnitsInLance);
      string targetTeamGuid = EncounterRules.TARGET_TEAM_ID;
      string spawnerName = $"Lance_Enemy_OpposingForce_{lanceGuid}";

      encounterRules.EncounterLogic.Add(new AddLanceToTargetTeam(lanceGuid, unitGuids, manuallySpecifiedLance));
      encounterRules.EncounterLogic.Add(new AddDestroyWholeUnitChunk(encounterRules, targetTeamGuid, lanceGuid, unitGuids,
         spawnerName, objectiveGuid, objectiveName, priority, isPrimaryObjective, displayToUser, lanceTags));
      if (!excludeFromAutocomplete) encounterRules.EncounterLogic.Add(new AddObjectiveToAutocompleteTrigger(objectiveGuid));
      encounterRules.EncounterLogic.Add(new SpawnLanceMembersAroundTarget(encounterRules, spawnerName, orientationTargetKey,
        SpawnLogic.LookDirection.AWAY_FROM_TARGET, mustBeBeyondDistance, mustBeWithinDistance));

      if (showObjectiveOnLanceDetected) {
        LanceVisibleToPlayerConditional lanceVisibleToPlayerConditional = ScriptableObject.CreateInstance<LanceVisibleToPlayerConditional>();
        lanceVisibleToPlayerConditional.TargetLanceGuid = lanceGuid;
        encounterRules.EncounterLogic.Add(new ShowObjectiveTrigger(MessageCenterMessageType.OnVisibilityIncreased, lanceGuid, objectiveGuid, false, lanceVisibleToPlayerConditional));
      }

      encounterRules.ObjectReferenceQueue.Add(spawnerName);
    }
  }
}
