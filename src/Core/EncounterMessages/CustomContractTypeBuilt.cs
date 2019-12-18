using BattleTech;

namespace MissionControl.Messages {
  public class CustomContractTypeBuilt : EncounterObjectMessage {

    public CustomContractTypeBuilt() : base() { }

    public override MessageCenterMessageType MessageType {
      get {
        return (MessageCenterMessageType)MessageTypes.OnCustomContractTypeBuilt;
      }
    }

    public override void FromJSON(string json) { }

    public override string GenerateJSONTemplate() { return ""; }

    public override string ToJSON() { return ""; }
  }
}
