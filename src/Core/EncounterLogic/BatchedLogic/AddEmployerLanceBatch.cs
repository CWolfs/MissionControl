using UnityEngine;
using System;
using System.Collections.Generic;

using MissionControl.Rules;
using MissionControl.Trigger;

namespace MissionControl.Logic {
  public class AddEmployerLanceBatch {
    public AddEmployerLanceBatch(EncounterRules encounterRules, string orientationTargetKey,
      SpawnLogic.LookDirection lookDirection, float minDistance, float maxDistance) {

        int numberOfUnitsInLance = 4;
        string lanceGuid = Guid.NewGuid().ToString();
        List<string> unitGuids = encounterRules.GenerateGuids(numberOfUnitsInLance);
        string employerTeamGuid = EncounterRules.EMPLOYER_TEAM_ID;
        string spawnerName = $"Lance_Ally_SupportingForce_{lanceGuid}";

        encounterRules.EncounterLogic.Add(new AddLanceToAllyTeam(lanceGuid, unitGuids));
        encounterRules.EncounterLogic.Add(new AddLanceSpawnChunk(employerTeamGuid, lanceGuid, unitGuids, spawnerName,
          "Spawns a non-objective related ally supporting lance"));
        encounterRules.EncounterLogic.Add(new SpawnLanceMembersAroundTarget(encounterRules, spawnerName, orientationTargetKey,
          lookDirection, minDistance, maxDistance));

        // encounterRules.EncounterLogic.Add(new AddDialogueChunk(ChunkLogic.DIALOGUE_ADDITIONAL_LANCE_ALLY_START_GUID, "AdditionalLanceAllyStart", "Start Conversation For Additional Lance Ally"));
        encounterRules.EncounterLogic.Add(new AddDialogueChunk(
          ChunkLogic.DIALOGUE_ADDITIONAL_LANCE_ALLY_START_GUID,
          "AdditionalLanceAllyStart",
          "Start Conversation For Additional Lance Ally",
          "DialogBucketDef_Universal_KillConfirmed"
        ));
      
        encounterRules.EncounterLogic.Add(new DialogTrigger(MessageCenterMessageType.OnEncounterIntroComplete, ChunkLogic.DIALOGUE_ADDITIONAL_LANCE_ALLY_START_GUID));

        encounterRules.ObjectReferenceQueue.Add(spawnerName);
    }
  }
}