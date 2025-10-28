using System;
using System.Collections.Generic;

using Harmony;

using BattleTech;

using MissionControl.Result;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(HostilityMatrix), "GetHostility")]
  [HarmonyPatch(new Type[] { typeof(string), typeof(string) })]
  public static class HostilityMatricGetHostilityPatch {
    [ThreadStatic]
    private static bool isExecuting;

    static bool Prefix(HostilityMatrix __instance, string teamGuidOne, string teamGuidTwo, ref Hostility __result) {
      // Prevent re-entry to avoid infinite recursion when accessing team.units/team.lances
      if (isExecuting) return true;

      try {
        isExecuting = true;

        if (UnityGameInstance.BattleTechGame.Combat != null) {
          // Check for specific relationship overrides first (SetRelationshipResult)
          Hostility? specificRelationship = CheckSpecificRelationships(teamGuidOne, teamGuidTwo);
          if (specificRelationship.HasValue) {
            // Main.LogDebug($"[HostilityMatricGetHostilityPatch] Applying specific relationship override: {specificRelationship.Value} between teams {teamGuidOne} and {teamGuidTwo}");
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
      } finally {
        isExecuting = false;
      }
    }

    private static Hostility? CheckSpecificRelationships(string teamGuidOne, string teamGuidTwo) {
      // Get the counter to know how many relationships to check
      string counterStr = MissionControl.Instance.GetGameLogicData(SetRelationshipResult.RELATIONSHIP_COUNTER);
      if (counterStr == null) return null;

      if (!int.TryParse(counterStr, out int counter)) return null;

      // Early exit if no relationships are configured
      if (counter <= 0) return null;

      // Track the most recent matching relationship
      SetRelationshipResult.RelationshipData bestMatch = null;

      // Iterate through all stored relationship data (most recent wins)
      for (int i = 0; i < counter; i++) {
        string relationshipKey = $"{SetRelationshipResult.RELATIONSHIP_DATA_PREFIX}{i}";
        string serializedData = MissionControl.Instance.GetGameLogicData(relationshipKey);

        if (serializedData == null) continue;

        SetRelationshipResult.RelationshipData data = SetRelationshipResult.DeserializeRelationshipData(serializedData);
        if (data == null) continue;

        // Check if this relationship applies to the team pairing
        bool subjectIsTeamOne = DoesRelationshipApplyToTeam(data, teamGuidOne);
        bool subjectIsTeamTwo = DoesRelationshipApplyToTeam(data, teamGuidTwo);

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

        // Skip same-team matches unless ALL_INCLUDING_OWN is used
        if (teamGuidOne == teamGuidTwo && !allowSameTeam) continue;

        // Store the match (most recent one wins)
        bestMatch = data;
      }

      // Return the most recent match found
      if (bestMatch != null) {
        Hostility relationship = (Hostility)Enum.Parse(typeof(Hostility), bestMatch.Relationship.ToUpper());
        // Main.LogDebug($"[HostilityMatricGetHostilityPatch.CheckSpecificRelationships] Found relationship match: {bestMatch.Relationship} for teams {teamGuidOne} <-> {teamGuidTwo}");
        return relationship;
      }

      return null;
    }

    private static bool DoesRelationshipApplyToTeam(SetRelationshipResult.RelationshipData data, string teamGuid) {
      // Only TEAM type is supported - check if the team GUID matches
      if (data.Type == "TEAM") {
        return data.Guids.Contains(teamGuid);
      }

      Main.Logger.LogError($"[HostilityMatricGetHostilityPatch] Unknown relationship type: {data.Type}. Only 'TEAM' is supported.");
      return false;
    }
  }
}