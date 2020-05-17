using BattleTech;

namespace MissionControl.Messages {
  public class BeforeSceneManipulationMessage : EncounterObjectMessage {

    public BeforeSceneManipulationMessage() : base() { }

    public override MessageCenterMessageType MessageType {
      get {
        return (MessageCenterMessageType)MessageTypes.BeforeSceneManipulation;
      }
    }

    public override void FromJSON(string json) { }

    public override string GenerateJSONTemplate() { return ""; }

    public override string ToJSON() { return ""; }
  }
}
