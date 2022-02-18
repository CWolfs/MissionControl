
using BattleTech;
using BattleTech.Framework;

using Harmony;

public static class ContractOverrideExtensions {
  public static string GetTeamLanceBelongsTo(this ContractOverride contractOverride, string lanceGuid) {
    TeamOverride player1Team = contractOverride.player1Team;
    if (player1Team.IsLanceInTeam(lanceGuid)) return "Player1";

    TeamOverride targetTeam = contractOverride.targetTeam;
    if (targetTeam.IsLanceInTeam(lanceGuid)) return "Target";

    TeamOverride employerTeam = contractOverride.employerTeam;
    if (employerTeam.IsLanceInTeam(lanceGuid)) return "Employer";

    TeamOverride employerAllyTeam = contractOverride.employersAllyTeam;
    if (employerAllyTeam.IsLanceInTeam(lanceGuid)) return "EmployersAlly";

    TeamOverride targetsAllyTeam = contractOverride.targetsAllyTeam;
    if (targetsAllyTeam.IsLanceInTeam(lanceGuid)) return "TargetsAlly";

    TeamOverride neutralToAllTeam = contractOverride.neutralToAllTeam;
    if (neutralToAllTeam.IsLanceInTeam(lanceGuid)) return "NeutralToAll";

    TeamOverride hostilToAllTeam = contractOverride.hostileToAllTeam;
    if (hostilToAllTeam.IsLanceInTeam(lanceGuid)) return "HostileToAll";

    TeamOverride player2Team = contractOverride.player2Team;
    if (player2Team.IsLanceInTeam(lanceGuid)) return "Player2";

    return null;
  }

  public static TeamOverride GetTeamOverrideLanceBelongsTo(this ContractOverride contractOverride, string lanceGuid) {
    TeamOverride player1Team = contractOverride.player1Team;
    if (player1Team.IsLanceInTeam(lanceGuid)) return player1Team;

    TeamOverride targetTeam = contractOverride.targetTeam;
    if (targetTeam.IsLanceInTeam(lanceGuid)) return targetTeam;

    TeamOverride employerTeam = contractOverride.employerTeam;
    if (employerTeam.IsLanceInTeam(lanceGuid)) return employerTeam;

    TeamOverride employerAllyTeam = contractOverride.employersAllyTeam;
    if (employerAllyTeam.IsLanceInTeam(lanceGuid)) return employerAllyTeam;

    TeamOverride targetsAllyTeam = contractOverride.targetsAllyTeam;
    if (targetsAllyTeam.IsLanceInTeam(lanceGuid)) return targetsAllyTeam;

    TeamOverride neutralToAllTeam = contractOverride.neutralToAllTeam;
    if (neutralToAllTeam.IsLanceInTeam(lanceGuid)) return neutralToAllTeam;

    TeamOverride hostilToAllTeam = contractOverride.hostileToAllTeam;
    if (hostilToAllTeam.IsLanceInTeam(lanceGuid)) return hostilToAllTeam;

    TeamOverride player2Team = contractOverride.player2Team;
    if (player2Team.IsLanceInTeam(lanceGuid)) return player2Team;

    return null;
  }
}