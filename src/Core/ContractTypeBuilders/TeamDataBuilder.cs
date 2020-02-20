using UnityEngine;

using Newtonsoft.Json.Linq;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using System;
using System.Collections.Generic;

using MissionControl.Result;
using MissionControl.Messages;

namespace MissionControl.ContractTypeBuilders {
  public class TeamDataBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject teamData;
    private string Team { get; set; }

    public TeamDataBuilder(ContractTypeBuilder contractTypeBuilder, JObject teamData) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.teamData = teamData;
    }

    public void Build() {
      this.Team = teamData.ContainsKey("Team") ? teamData["Team"].ToString() : null;

      if (this.Team == null) {
        Main.Logger.LogError($"[TeamDataBuilder.{contractTypeBuilder.ContractTypeKey}] No Team provided for the team data");
        return;
      }

      if (teamData.ContainsKey("ShareVisionWithAllies")) {
        BuildShareVisionWithAllies();
      }
    }

    private void BuildShareVisionWithAllies() {
      Main.LogDebug($"[TeamDataBuilder] Building 'ShareVisionWithAllies' team data handler for team '{this.Team}'");
      bool shareVisionWithAllies = (bool)teamData["ShareVisionWithAllies"];
      SetTeamShareVisionWithAlliesResult visionShare = ScriptableObject.CreateInstance<SetTeamShareVisionWithAlliesResult>();

      visionShare.Team = this.Team;
      visionShare.ShareVision = shareVisionWithAllies;

      MessageCenter messageCenter = UnityGameInstance.BattleTechGame.MessageCenter;
      messageCenter.AddSubscriber((MessageCenterMessageType)MessageTypes.BeforeSceneManipulation, new ReceiveMessageCenterMessage(visionShare.BeforeSceneManipulation));
    }
  }
}