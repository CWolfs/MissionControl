using System.Collections.Generic;

using HBS.Collections;

using BattleTech;

using MissionControl.AI;

namespace MissionControl.Result {
	public class IssueCustomAIOrderResult : EncounterResult {
    public IssueAIOrderTo issueAIOrderTo;
		public TagSet requiredReceiverTags = new TagSet();
		public CustomAIOrder aiOrder = new CustomAIOrder();

		public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
			Main.Logger.Log("[IssueCustomAIOrderResult] Triggered");
			if ((this.issueAIOrderTo & IssueAIOrderTo.ToUnit) != IssueAIOrderTo.INVALID_UNSET) {
				List<ITaggedItem> objectsOfTypeWithTagSet = this.combat.ItemRegistry.GetObjectsOfTypeWithTagSet(TaggedObjectType.Unit, this.requiredReceiverTags);
				for (int i = 0; i < objectsOfTypeWithTagSet.Count; i++) {
					AbstractActor abstractActor = objectsOfTypeWithTagSet[i] as AbstractActor;
          AiManager.Instance.IssueAiOrder("UNIT", abstractActor.GUID, this.aiOrder);
				}
			}

			if ((this.issueAIOrderTo & IssueAIOrderTo.ToLance) != IssueAIOrderTo.INVALID_UNSET) {
				List<ITaggedItem> objectsOfTypeWithTagSet2 = this.combat.ItemRegistry.GetObjectsOfTypeWithTagSet(TaggedObjectType.Lance, this.requiredReceiverTags);
				for (int j = 0; j < objectsOfTypeWithTagSet2.Count; j++) {
					Lance lance = objectsOfTypeWithTagSet2[j] as Lance;
					AiManager.Instance.IssueAiOrder("LANCE", lance.GUID, this.aiOrder);
				}
			}

			if ((this.issueAIOrderTo & IssueAIOrderTo.ToTeam) != IssueAIOrderTo.INVALID_UNSET) {
				List<ITaggedItem> objectsOfTypeWithTagSet3 = this.combat.ItemRegistry.GetObjectsOfTypeWithTagSet(TaggedObjectType.Team, this.requiredReceiverTags);
				for (int k = 0; k < objectsOfTypeWithTagSet3.Count; k++) {
					Team team = objectsOfTypeWithTagSet3[k] as Team;
					AiManager.Instance.IssueAiOrder("TEAM", team.GUID, this.aiOrder);
				}
			}
		}
	}
}
