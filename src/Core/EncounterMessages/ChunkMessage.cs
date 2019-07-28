using BattleTech;

namespace MissionControl.Messages {
	public class ChunkMessage : EncounterObjectMessage {

		public ChunkMessage(string objectiveGUID) : base(objectiveGUID, objectiveGUID) {}

		public string ChunkGuid {
			get {
				return base.actingObjectGuid;
			}
		}

		public override MessageCenterMessageType MessageType {
			get {
				return MessageCenterMessageType.OnObjectiveUpdated;
			}
		}

    public override void FromJSON(string json) { }

    public override string GenerateJSONTemplate() { return ""; }

    public override string ToJSON() { return ""; }
  }
}
