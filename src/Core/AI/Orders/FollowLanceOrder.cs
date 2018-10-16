using HBS.Collections;

namespace MissionControl.AI {
	public class FollowLanceOrder : CustomAIOrder {
		public override string CustomOrderType {
			get {
				return "FOLLOW_LANCE";
			}
		}

		public TagSet TargetEncounterTags { get; set; } = new TagSet();
		public Lance TargetToFollow { get; set; }
		public bool ShouldSprint { get; set; } = false;
		public float FollowLanceRadius { get; set; } = 30f;
	}
}
