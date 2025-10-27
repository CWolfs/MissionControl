using BattleTech;
using BattleTech.Framework;

using HBS.Collections;

using System;
using System.Collections.Generic;

namespace MissionControl.Result {
  public class SetRelationshipResult : EncounterResult {
    public static string RELATIONSHIP_DATA_PREFIX = "SET_RELATIONSHIP_";
    public static string RELATIONSHIP_COUNTER = "SET_RELATIONSHIP_COUNTER";
    private static int relationshipCounter = 0;

    public string[] Teams { get; set; } // Team names like "Player1", "Employer", etc.
    public string[] LanceSpawnerGuids { get; set; }
    public string[] UnitGuids { get; set; }
    public string[] Tags { get; set; }
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

      // Determine which targeting mode is being used
      // Order of precedence: Unit GUIDs > Tags > Lance Spawner GUIDs > Teams
      if (UnitGuids?.Length > 0) {
        Main.LogDebug($"[SetRelationshipResult] Using Unit GUIDs mode");
        HashSet<string> unitGuids = new HashSet<string>(UnitGuids);
        StoreRelationshipData("UNIT", String.Join(",", unitGuids), targetTeamGuid);
      } else if (Tags?.Length > 0) {
        Main.LogDebug($"[SetRelationshipResult] Using Tags mode with tags: {String.Join(", ", Tags)}");
        List<ICombatant> combatants = ObjectiveGameLogic.GetTaggedCombatants(UnityGameInstance.BattleTechGame.Combat, new TagSet(Tags));
        Main.LogDebug($"[SetRelationshipResult] Found '{combatants.Count}' combatants with tags");

        HashSet<string> unitGuids = new HashSet<string>();
        foreach (ICombatant combatant in combatants) {
          if (combatant is AbstractActor) {
            unitGuids.Add(combatant.GUID);
            Main.LogDebug($"[SetRelationshipResult] Added unit GUID: {combatant.GUID}");
          }
        }
        StoreRelationshipData("UNIT", String.Join(",", unitGuids), targetTeamGuid);

      } else if (LanceSpawnerGuids?.Length > 0) {
        Main.LogDebug("[SetRelationshipResult] Using Lance Spawner GUIDs mode");
        HashSet<string> lanceSpawnerGuids = new HashSet<string>();

        foreach (string lanceGuid in LanceSpawnerGuids) {
          LanceSpawnerGameLogic spawnerGameLogic = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID<LanceSpawnerGameLogic>(lanceGuid);
          if (spawnerGameLogic != null) {
            lanceSpawnerGuids.Add(lanceGuid);
            Main.LogDebug($"[SetRelationshipResult] Added lance GUID: {lanceGuid}");
          } else {
            Main.Logger.LogWarning($"[SetRelationshipResult] Could not find lance spawner with GUID: {lanceGuid}");
          }
        }
        StoreRelationshipData("LANCE", String.Join(",", lanceSpawnerGuids), targetTeamGuid);

      } else if (Teams?.Length > 0) {
        Main.LogDebug($"[SetRelationshipResult] Using Team names mode");
        HashSet<string> teamGuids = new HashSet<string>();

        foreach (string teamName in Teams) {
          string teamGuid = TeamUtils.GetTeamGuid(teamName);
          if (teamGuid != null) {
            teamGuids.Add(teamGuid);
          }
        }
        StoreRelationshipData("TEAM", String.Join(",", teamGuids), targetTeamGuid);

      } else {
        Main.Logger.LogError($"[SetRelationshipResult] No targeting mode specified! Must set Teams, LanceSpawnerGuids, UnitGuids, or Tags");
        return;
      }

      Main.LogDebug($"[SetRelationshipResult] Stored relationship '{relationshipId}'");
    }

    private void StoreRelationshipData(string type, string guids, string targetTeamGuid) {
      // Simple serialization: Relationship|Type|Guids|TargetTeamGuid
      string targetTeam = targetTeamGuid ?? "";
      string serializedData = $"{Relationship}|{type}|{guids}|{targetTeam}";
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

    public class RelationshipData {
      public string Relationship { get; set; }
      public string Type { get; set; } // "UNIT", "LANCE", or "TEAM"
      public HashSet<string> Guids { get; set; }
      public string TargetTeamGuid { get; set; }
    }
  }
}
