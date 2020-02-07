using UnityEngine;

using BattleTech;
using BattleTech.Framework;

using HBS.Util;

using MissionControl.Data;
using MissionControl.Rules;

using Harmony;

namespace MissionControl.LogicComponents.Placers {
  public class SwapTeamFactionGameLogic : EncounterObjectGameLogic {

    [SerializeField]
    public string team1Guid { get; set; } = "UNSET";

    [SerializeField]
    public string team2Guid { get; set; } = "UNSET";

    public override TaggedObjectType Type {
      get {
        return (TaggedObjectType)MCTaggedObjectType.SwapTeamFaction;
      }
    }

    public override void AlwaysInit(CombatGameState combat) {
      base.AlwaysInit(combat);
      this.messageMemory.TrackMethod(MessageCenterMessageType.OnMissionSucceeded, new ReceiveMessageCenterMessage(this.OnMissionSucceeded));
    }

    protected override void SubscribeToMessages(bool shouldAdd) {
      this.messageMemory.Subscribe(MessageCenterMessageType.OnMissionSucceeded, new ReceiveMessageCenterMessage(this.OnMissionSucceeded), shouldAdd);
      base.SubscribeToMessages(shouldAdd);
    }

    private void OnMissionSucceeded(MessageCenterMessage message) {
      if (this.gameObject.GetComponentInParent<EncounterChunkGameLogic>().startingStatus == EncounterObjectStatus.ControlledByContract) {
        Main.LogDebug($"[SwapTeamFactionGameLogic.OnMissionSucceeded] Skipping as Chunk is set to not be enabled in the contract json");
        return;
      }

      SwapTeamFactions();
    }

    private void SwapTeamFactions() {
      Contract contract = MissionControl.Instance.CurrentContract;
      FactionValue faction1 = contract.GetTeamFaction(team1Guid);
      FactionValue faction2 = contract.GetTeamFaction(team2Guid);
      int originalFaction1Id = faction1.ID;
      int originalFaction2Id = faction2.ID;

      Main.LogDebug($"[SwapTeamFactionGameLogic.SwapTeamFactions]) Swapping factions '{team1Guid}:{faction1.Name}' with '{team2Guid}:{faction2.Name}'");

      MissionControl.Instance.CurrentContract.SetTeamFaction(team1Guid, originalFaction2Id);
      MissionControl.Instance.CurrentContract.SetTeamFaction(team2Guid, originalFaction1Id);

      AccessTools.Method(typeof(ContractOverride), "AssignFactionsToTeams").Invoke(contract.Override, new object[] { contract.TeamFactionIDs });
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
  }
}
