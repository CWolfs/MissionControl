using System;
using System.Collections.Generic;

using Harmony;

using BattleTech;

using MissionControl.Result;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(HostilityMatrix), "GetHostility")]
  [HarmonyPatch(new Type[] { typeof(string), typeof(string) })]
  public static class HostilityMatricGetHostilityPatch {
    static bool Prefix(HostilityMatrix __instance, string teamGuidOne, string teamGuidTwo, ref Hostility __result) {
      if (UnityGameInstance.BattleTechGame.Combat != null) {
        // Check for specific relationship overrides first (SetRelationshipResult)
        Hostility? specificRelationship = CheckSpecificRelationships(teamGuidOne, teamGuidTwo);
        if (specificRelationship.HasValue) {
          __result = specificRelationship.Value;
          return false;
        }

        // Fall back to global all-teams relationship (SetAllTeamsRelationshipResult)
        string enableAllTeamsRelationship = MissionControl.Instance.GetGameLogicData(SetAllTeamsRelationshipResult.ENABLE_ALL_TEAMS_RELATIONSHIP);

        if (enableAllTeamsRelationship != null && enableAllTeamsRelationship == "true") {
          if (teamGuidOne == teamGuidTwo) return true;

          string relationshipRaw = MissionControl.Instance.GetGameLogicData(SetAllTeamsRelationshipResult.ALL_TEAMS_RELATIONSHIP);
          Hostility relationship = (Hostility)Enum.Parse(typeof(Hostility), relationshipRaw.ToUpper());
          __result = relationship;
          return false;
        }
      }

      return true;
    }

    private static Hostility? CheckSpecificRelationships(string teamGuidOne, string teamGuidTwo) {
      // Don't override same-team relationships (unless ALL_INCLUDING_OWN is used)
      if (teamGuidOne == teamGuidTwo) return null;

      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;

      // Get the counter to know how many relationships to check
      string counterStr = MissionControl.Instance.GetGameLogicData(SetRelationshipResult.RELATIONSHIP_COUNTER);
      if (counterStr == null) return null;

      if (!int.TryParse(counterStr, out int counter)) return null;

      // Track the best matching relationship (highest priority + most recent)
      SetRelationshipResult.RelationshipData bestMatch = null;
      int bestPriority = -1;
      int bestCounter = -1;

      // Iterate through all stored relationship data
      for (int i = 0; i < counter; i++) {
        string relationshipKey = $"{SetRelationshipResult.RELATIONSHIP_DATA_PREFIX}{i}";
        string serializedData = MissionControl.Instance.GetGameLogicData(relationshipKey);

        if (serializedData == null) continue;

        SetRelationshipResult.RelationshipData data = SetRelationshipResult.DeserializeRelationshipData(serializedData);
        if (data == null) continue;

        // Check if this relationship applies to the team pairing
        bool subjectIsTeamOne = DoesRelationshipApplyToTeam(data, teamGuidOne, combat);
        bool subjectIsTeamTwo = DoesRelationshipApplyToTeam(data, teamGuidTwo, combat);

        // Check if the relationship matches this team pairing
        bool isMatch;
        bool allowSameTeam = false;

        if (data.TargetTeamGuid == "ALL_INCLUDING_OWN") {
          // ALL_INCLUDING_OWN: Subject matches one team, target can be any team (including same)
          isMatch = subjectIsTeamOne || subjectIsTeamTwo;
          allowSameTeam = true;
        } else if (data.TargetTeamGuid == "ALL") {
          // ALL: Subject matches one team, target can be any team except same team
          isMatch = subjectIsTeamOne || subjectIsTeamTwo;
        } else {
          // Specific target: Subject matches one team AND target matches the other team (bidirectional)
          isMatch = (subjectIsTeamOne && teamGuidTwo == data.TargetTeamGuid) ||
                    (subjectIsTeamTwo && teamGuidOne == data.TargetTeamGuid);
        }

        if (!isMatch) continue;

        // Handle same-team override for ALL_INCLUDING_OWN
        if (allowSameTeam && teamGuidOne == teamGuidTwo) {
          // This is a same-team relationship with ALL_INCLUDING_OWN, allow it
          Hostility relationship = (Hostility)Enum.Parse(typeof(Hostility), data.Relationship.ToUpper());
          return relationship; // Return immediately since this overrides the same-team check
        }

        // Calculate priority (UNIT=4, TAG=3, LANCE=2, TEAM=1)
        int priority = GetRelationshipPriority(data.Type);

        // Check if this is a better match than what we've found so far
        // Priority order: Higher priority level wins, then most recent (higher counter) wins
        if (priority > bestPriority || (priority == bestPriority && i > bestCounter)) {
          bestMatch = data;
          bestPriority = priority;
          bestCounter = i;
        }
      }

      // Return the best match found
      if (bestMatch != null) {
        Hostility relationship = (Hostility)Enum.Parse(typeof(Hostility), bestMatch.Relationship.ToUpper());
        return relationship;
      }

      return null;
    }

    private static int GetRelationshipPriority(string type) {
      switch (type) {
        case "UNIT": return 4; // Highest priority
        case "LANCE": return 2;
        case "TEAM": return 1; // Lowest priority
        default:
          Main.Logger.LogError($"[HostilityMatricGetHostilityPatch] Unknown relationship type: {type}");
          return 0;
      }
    }

    private static bool DoesRelationshipApplyToTeam(SetRelationshipResult.RelationshipData data, string teamGuid, CombatGameState combat) {
      Team team = combat.ItemRegistry.GetItemByGUID<Team>(teamGuid);
      if (team == null) return false;

      // Check based on the type specified in the data
      switch (data.Type) {
        case "UNIT":
          // Check if any unit in the team matches the unit GUIDs
          foreach (AbstractActor unit in team.units) {
            if (data.Guids.Contains(unit.GUID)) {
              return true;
            }
          }
          return false;

        case "LANCE":
          // Check if any lance in the team matches the lance spawner GUIDs
          // Lance.GUID format is "{spawnerGuid}.Lance", so we need to strip the suffix
          foreach (Lance lance in team.lances) {
            string lanceSpawnerGuid = lance.GUID.Replace(".Lance", "");
            if (data.Guids.Contains(lanceSpawnerGuid)) {
              return true;
            }
          }
          return false;

        case "TEAM":
          // Check if the team GUID matches
          return data.Guids.Contains(teamGuid);

        default:
          Main.Logger.LogError($"[HostilityMatricGetHostilityPatch] Unknown relationship type: {data.Type}");
          return false;
      }
    }
  }
}