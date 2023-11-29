using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Result {
  public class SwapTeamsResult : EncounterResult {
    public string Team1GUID { get; set; }
    public string Team2GUID { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Contract contract = MissionControl.Instance.CurrentContract;
      FactionValue faction1 = contract.GetTeamFaction(Team1GUID);
      FactionValue faction2 = contract.GetTeamFaction(Team2GUID);
      int originalFaction1Id = faction1.ID;
      int originalFaction2Id = faction2.ID;

      Main.LogDebug($"[SwapTeamsResult] Swapping factions '{Team1GUID}:{faction1.Name}' with '{Team2GUID}:{faction2.Name}'");
      TeamOverride employer = contract.Override.employerTeam;
      TeamOverride target = contract.Override.targetTeam;

      contract.Override.employerTeam = target;
      contract.Override.targetTeam = employer;

      MissionControl.Instance.CurrentContract.SetTeamFaction(Team1GUID, originalFaction2Id);
      MissionControl.Instance.CurrentContract.SetTeamFaction(Team2GUID, originalFaction1Id);

      contract.Override.RunMadLibs(UnityGameInstance.BattleTechGame.DataManager);
    }
  }
}
