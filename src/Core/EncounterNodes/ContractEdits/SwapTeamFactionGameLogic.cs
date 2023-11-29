using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using HBS.Util;

using MissionControl.Data;
using MissionControl.LogicComponents;

namespace MissionControl.EncounterNodes.ContractEdits {
  /**
  * Ideally this would be a trigger, condition and result combination
  * - Enter region
  * - Send Message
  * - Condition is met
  * - Result runs which runs this class
  */
  public class SwapTeamFactionGameLogic : EncounterObjectGameLogic, ExecutableGameLogic {

    [SerializeField]
    public string team1Guid { get; set; } = "UNSET";

    [SerializeField]
    public string team2Guid { get; set; } = "UNSET";

    public override TaggedObjectType Type {
      get {
        return (TaggedObjectType)MCTaggedObjectType.SwapTeamFaction;
      }
    }

    private void SwapTeamFactions() {
      Contract contract = MissionControl.Instance.CurrentContract;
      FactionValue faction1 = contract.GetTeamFaction(team1Guid);
      FactionValue faction2 = contract.GetTeamFaction(team2Guid);
      int originalFaction1Id = faction1.ID;
      int originalFaction2Id = faction2.ID;

      Main.LogDebug($"[SwapTeamFactionGameLogic.SwapTeamFactions] Swapping factions '{team1Guid}:{faction1.Name}' with '{team2Guid}:{faction2.Name}'");
      TeamOverride employer = contract.Override.employerTeam;
      TeamOverride target = contract.Override.targetTeam;

      contract.Override.employerTeam = target;
      contract.Override.targetTeam = employer;

      MissionControl.Instance.CurrentContract.SetTeamFaction(team1Guid, originalFaction2Id);
      MissionControl.Instance.CurrentContract.SetTeamFaction(team2Guid, originalFaction1Id);

      contract.Override.RunMadLibs(UnityGameInstance.BattleTechGame.DataManager);
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<SwapTeamFactionGameLogic>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<SwapTeamFactionGameLogic>(new SwapTeamFactionGameLogic());
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<SwapTeamFactionGameLogic>(this);
    }

    public void Execute() {
      SwapTeamFactions();
    }
  }
}
