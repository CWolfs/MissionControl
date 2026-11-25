using BattleTech;
using BattleTech.Framework;

using System.Collections.Generic;

public static class TeamUtils {
  public const string PLAYER_TEAM_ID = "bf40fd39-ccf9-47c4-94a6-061809681140";
  public const string PLAYER_2_TEAM_ID = "757173dd-b4e1-4bb5-9bee-d78e623cc867";
  public const string EMPLOYER_TEAM_ID = "ecc8d4f2-74b4-465d-adf6-84445e5dfc230";
  public const string EMPLOYER_ALLY_TEAM_ID = "70af7e7f-39a8-4e81-87c2-bd01dcb01b5e";
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
      case "EmployersAlly": return EMPLOYER_ALLY_TEAM_ID;
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

  public static TeamOverride GetTeamOverride(ContractOverride contractOverride, string teamGuid) {
    if (contractOverride == null || string.IsNullOrEmpty(teamGuid)) return null;

    switch (teamGuid) {
      case PLAYER_TEAM_ID: return contractOverride.player1Team;
      case PLAYER_2_TEAM_ID: return contractOverride.player2Team;
      case EMPLOYER_TEAM_ID: return contractOverride.employerTeam;
      case EMPLOYER_ALLY_TEAM_ID: return contractOverride.employersAllyTeam;
      case TARGET_TEAM_ID: return contractOverride.targetTeam;
      case TARGETS_ALLY_TEAM_ID: return contractOverride.targetsAllyTeam;
      case NEUTRAL_TO_ALL_TEAM_ID: return contractOverride.neutralToAllTeam;
      case HOSTILE_TO_ALL_TEAM_ID: return contractOverride.hostileToAllTeam;
      default: {
        MissionControl.Main.Logger.LogError($"[GetTeamOverride] Unknown Team GUID '{teamGuid}'");
        return null;
      }
    }
  }

  public static void SwapTeamFactions(Contract contract, string team1Guid, string team2Guid) {
    if (contract == null || contract.Override == null) {
      MissionControl.Main.Logger.LogError("[SwapTeamFactions] CurrentContract or ContractOverride is null.");
      return;
    }

    if (string.IsNullOrEmpty(team1Guid) || string.IsNullOrEmpty(team2Guid) || team1Guid == team2Guid) {
      MissionControl.Main.Logger.LogWarning($"[SwapTeamFactions] Invalid or identical team GUIDs ('{team1Guid}', '{team2Guid}'). Skipping swap.");
      return;
    }

    TeamOverride teamOverride1 = GetTeamOverride(contract.Override, team1Guid);
    TeamOverride teamOverride2 = GetTeamOverride(contract.Override, team2Guid);
    if (teamOverride1 == null || teamOverride2 == null) {
      MissionControl.Main.Logger.LogError($"[SwapTeamFactions] Could not resolve both team overrides for '{team1Guid}' and '{team2Guid}'.");
      return;
    }

    FactionValue faction1 = contract.GetTeamFaction(team1Guid);
    FactionValue faction2 = contract.GetTeamFaction(team2Guid);
    if (faction1.IsInvalidUnset) faction1 = teamOverride1.FactionValue;
    if (faction2.IsInvalidUnset) faction2 = teamOverride2.FactionValue;

    MissionControl.Main.LogDebug($"[SwapTeamFactions] Swapping factions '{team1Guid}:{faction1.Name}' with '{team2Guid}:{faction2.Name}'");

    string originalTeam1Faction = teamOverride1.faction;
    teamOverride1.faction = teamOverride2.faction;
    teamOverride2.faction = originalTeam1Faction;

    string originalTeam1LeaderCastDefId = teamOverride1.teamLeaderCastDefId;
    teamOverride1.teamLeaderCastDefId = teamOverride2.teamLeaderCastDefId;
    teamOverride2.teamLeaderCastDefId = originalTeam1LeaderCastDefId;

    contract.SetTeamFaction(team1Guid, faction2.ID);
    contract.SetTeamFaction(team2Guid, faction1.ID);

    contract.Override.RunMadLibs(UnityGameInstance.BattleTechGame.DataManager);
  }
}