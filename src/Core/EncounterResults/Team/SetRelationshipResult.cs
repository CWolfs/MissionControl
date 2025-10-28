using BattleTech;

using System;
using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetRelationshipResult : EncounterResult {
    public static string RELATIONSHIP_DATA_PREFIX = "SET_RELATIONSHIP_";
    public static string RELATIONSHIP_COUNTER = "SET_RELATIONSHIP_COUNTER";
    private static int relationshipCounter = 0;

    public string[] Teams { get; set; } // Team names like "Player1", "Employer", etc.
    public string Relationship { get; set; }
    public string TargetTeam { get; set; } // Required. The team name this relationship applies to. Can be a specific team name, "ALL" (all teams except subject's own), or "ALL_INCLUDING_OWN" (all teams including subject's own)

    private string relationshipId;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      // Validate TargetTeam is specified
      if (string.IsNullOrEmpty(TargetTeam)) {
        Main.Logger.LogError("[SetRelationshipResult] TargetTeam is required but was not specified!");
        return;
      }

      // Get and increment counter
      string counterStr = MissionControl.Instance.GetGameLogicData(RELATIONSHIP_COUNTER);
      if (counterStr != null) {
        _ = int.TryParse(counterStr, out relationshipCounter);
      }
      relationshipId = $"{RELATIONSHIP_DATA_PREFIX}{relationshipCounter}";
      MissionControl.Instance.SetGameLogicData(RELATIONSHIP_COUNTER, (relationshipCounter + 1).ToString());

      Main.LogDebug($"[SetRelationshipResult] Setting relationship '{Relationship}' to target '{TargetTeam}' with ID '{relationshipId}'");

      // Handle special ALL keywords, otherwise resolve to actual team GUID
      string targetTeamGuid;
      if (TargetTeam == "ALL" || TargetTeam == "ALL_INCLUDING_OWN") {
        targetTeamGuid = TargetTeam; // Pass through as-is for patch to handle
      } else {
        targetTeamGuid = TeamUtils.GetTeamGuid(TargetTeam);
        if (targetTeamGuid == null) {
          Main.Logger.LogError($"[SetRelationshipResult] Unknown TargetTeam: '{TargetTeam}'. Valid values are team names (Player1, Employer, etc.), 'ALL', or 'ALL_INCLUDING_OWN'");
          return;
        }
      }

      // Validate Teams is specified
      if (Teams == null || Teams.Length == 0) {
        Main.Logger.LogError("[SetRelationshipResult] Teams is required but was not specified!");
        return;
      }

      // Convert team names to GUIDs
      Main.LogDebug($"[SetRelationshipResult] Setting relationship for teams: {string.Join(", ", Teams)}");
      HashSet<string> teamGuids = new HashSet<string>();

      foreach (string teamName in Teams) {
        string teamGuid = TeamUtils.GetTeamGuid(teamName);
        if (teamGuid != null) {
          teamGuids.Add(teamGuid);
          Main.LogDebug($"[SetRelationshipResult] Added team GUID: {teamGuid}");
        } else {
          Main.Logger.LogWarning($"[SetRelationshipResult] Unknown team name: {teamName}");
        }
      }

      if (teamGuids.Count == 0) {
        Main.Logger.LogError("[SetRelationshipResult] No valid teams found!");
        return;
      }

      // Store relationship data
      StoreRelationshipData("TEAM", string.Join(",", teamGuids), targetTeamGuid);
      Main.LogDebug($"[SetRelationshipResult] Stored relationship '{relationshipId}'");

      // Alert affected teams
      AlertAffectedUnits(teamGuids);
    }

    private void StoreRelationshipData(string type, string guids, string targetTeamGuid) {
      // Simple serialization: Relationship|Type|Guids|TargetTeamGuid
      string serializedData = $"{Relationship}|{type}|{guids}|{targetTeamGuid}";
      MissionControl.Instance.SetGameLogicData(relationshipId, serializedData);
    }

    public static RelationshipData DeserializeRelationshipData(string serialized) {
      string[] parts = serialized.Split('|');
      if (parts.Length != 4) {
        Main.Logger.LogError($"[SetRelationshipResult] Invalid serialized relationship data: {serialized}");
        return null;
      }

      RelationshipData data = new RelationshipData {
        Relationship = parts[0],
        Type = parts[1],
        Guids = new HashSet<string>(),
        TargetTeamGuid = string.IsNullOrEmpty(parts[3]) ? null : parts[3]
      };

      if (!string.IsNullOrEmpty(parts[2])) {
        foreach (string guid in parts[2].Split(',')) {
          if (!string.IsNullOrEmpty(guid)) {
            data.Guids.Add(guid);
          }
        }
      }

      return data;
    }

    private void AlertAffectedUnits(HashSet<string> teamGuids) {
      Main.LogDebug("[SetRelationshipResult.AlertAffectedUnits] Alerting affected teams to re-evaluate relationships");

      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;
      int lancesAlerted = 0;

      // Alert all lances in the specified teams
      foreach (string teamGuid in teamGuids) {
        Team team = combat.ItemRegistry.GetItemByGUID<Team>(teamGuid);
        if (team != null) {
          foreach (Lance lance in team.lances) {
            Main.LogDebug($"[SetRelationshipResult.AlertAffectedUnits] Alerting lance: {lance.DisplayName} in team {team.Name}");
            lance.BroadcastAlert();
            lancesAlerted++;
          }
        }
      }

      Main.LogDebug($"[SetRelationshipResult.AlertAffectedUnits] Alerted {lancesAlerted} lance(s). Units will recognize new relationships on their next turn.");
    }

    public class RelationshipData {
      public string Relationship { get; set; }
      public string Type { get; set; } // "TEAM"
      public HashSet<string> Guids { get; set; }
      public string TargetTeamGuid { get; set; }
    }
  }
}
