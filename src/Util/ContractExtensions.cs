
using BattleTech;

public static class ContractExtensions {
  public static void SetTeamFaction(this Contract contract, string teamGuid, int factionId) {
    contract.TeamFactionIDs[teamGuid] = factionId;
  }
}