using UnityEngine;

using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using HBS.Collections;

using MissionControl.AI;
using MissionControl.Result;
using MissionControl.Conditional;
using MissionControl.Logic;

namespace MissionControl.Trigger {
  public class IssueFollowLanceOrderTrigger : EncounterTrigger {
    private List<string> receiverTags;
    private IssueAIOrderTo receiverType;
    private List<string> targetTags;
    private TaggedObjectType targetType;

    public IssueFollowLanceOrderTrigger(List<string> receiverTags, IssueAIOrderTo receiverType, List<string> targetTags, TaggedObjectType targetType) {
      this.receiverTags = receiverTags;
      this.receiverType = receiverType;
      this.targetTags = targetTags;
      this.targetType = targetType;
    }

    // TODO: Replace the bottom info with the passed in args for this class
    public override void Run(RunPayload payload) {
      Main.Logger.Log("[IssueFollowLanceOrderTrigger] Running trigger");
      EncounterLayerData encounterData = MissionControl.Instance.EncounterLayerData;
      SmartTriggerResponse onEncounterLoadIssueOrder = new SmartTriggerResponse();
      onEncounterLoadIssueOrder.inputMessage = MessageCenterMessageType.OnEncounterBegin;
      onEncounterLoadIssueOrder.designName = "Issue Follow Lance AI order on Encounter Start";
      onEncounterLoadIssueOrder.conditionalbox = new EncounterConditionalBox(new AlwaysTrueConditional());

      FollowLanceOrder followOrder = new FollowLanceOrder();
      followOrder.EncounterTags.Add("Player 1");
      
      IssueCustomAIOrderResult issueOrder = ScriptableObject.CreateInstance<IssueCustomAIOrderResult>();
      issueOrder.issueAIOrderTo = IssueAIOrderTo.ToLance;
      issueOrder.requiredReceiverTags.Add("Employer");
      issueOrder.aiOrder = followOrder;

      onEncounterLoadIssueOrder.resultList.contentsBox.Add(new EncounterResultBox(issueOrder));
      encounterData.responseGroup.triggerList.Add(onEncounterLoadIssueOrder);
    }
  }
}