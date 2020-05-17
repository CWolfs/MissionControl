using BattleTech;

namespace MissionControl.Messages {
  public class EncounterObjectStateChangeMessage : EncounterObjectMessage {
    public EncounterObjectStatus State { get; set; } = EncounterObjectStatus.Nothing;

    public EncounterObjectStateChangeMessage(string guid, EncounterObjectStatus state) : base(guid, guid) {
      this.State = state;
    }

    public string EncounterGuid {
      get {
        return base.actingObjectGuid;
      }
    }

    public override MessageCenterMessageType MessageType {
      get {
        return (MessageCenterMessageType)MessageTypes.OnEncounterStateChanged;
      }
    }

    public override void FromJSON(string json) { }

    public override string GenerateJSONTemplate() { return ""; }

    public override string ToJSON() { return ""; }
  }
}
