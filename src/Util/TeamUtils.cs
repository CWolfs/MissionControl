using BattleTech;

using System.Collections.Generic;

public static class TeamUtils {
  public const string PLAYER_TEAM_ID = "bf40fd39-ccf9-47c4-94a6-061809681140";
  public const string PLAYER_2_TEAM_ID = "757173dd-b4e1-4bb5-9bee-d78e623cc867";
  public const string EMPLOYER_TEAM_ID = "ecc8d4f2-74b4-465d-adf6-84445e5dfc230";
  public const string TARGET_TEAM_ID = "be77cadd-e245-4240-a93e-b99cc98902a5";
  public const string TARGETS_ALLY_TEAM_ID = "31151ed6-cfc2-467e-98c4-9ae5bea784cf";
  public const string NEUTRAL_TO_ALL_TEAM_ID = "61612bb3-abf9-4586-952a-0559fa9dcd75";
  public const string HOSTILE_TO_ALL_TEAM_ID = "3c9f3a20-ab03-4bcb-8ab6-b1ef0442bbf0";
  public const string WORLD_TEAM_ID = "421027ec-8480-4cc6-bf01-369f84a22012";

  public static string GetTeamGuid(string teamName) {
    switch (teamName) {
      case "Player1": return PLAYER_TEAM_ID;
      case "Player2": return PLAYER_2_TEAM_ID;
      case "Employer": return EMPLOYER_TEAM_ID;
      case "Target": return TARGET_TEAM_ID;
      case "TargetAlly": return TARGETS_ALLY_TEAM_ID;
      case "NeutralToAll": return NEUTRAL_TO_ALL_TEAM_ID;
      case "HostileToAll": return HOSTILE_TO_ALL_TEAM_ID;
      case "World": return WORLD_TEAM_ID;
      default: {
        MissionControl.Main.Logger.LogError($"[GetTeamGuid] Unknown Team Name of '{teamName}'");
        return null;
      }
    }
  }

  public static Team GetTeam(string teamGUID) {
    List<Team> teams = UnityGameInstance.Instance.Game.Combat.Teams;
    Team team = teams.Find((Team x) => x.GUID == teamGUID);

    if (team != null) return team;

    MissionControl.Main.Logger.LogError($"[GetTeam] Attempted to get team with guid '{teamGUID}' but no team was found.");
    return null;
  }
}