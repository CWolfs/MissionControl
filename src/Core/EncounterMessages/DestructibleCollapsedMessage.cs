using System;
using HBS.Util;

namespace MissionControl.Messages {
  public class DestructibleCollapsedMessage : EncounterObjectMessage {

    public DestructibleObject Destructible { get; set; }

    public override MessageCenterMessageType MessageType {
      get {
        return (MessageCenterMessageType)MessageTypes.OnDestructibleCollapsed;
      }
    }

    public DestructibleCollapsedMessage(DestructibleObject destructible) : base(string.Empty, string.Empty) {
      Destructible = destructible;
    }

    public override void LoadComplete() {
      base.LoadComplete();
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<DestructibleCollapsedMessage>(this);
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<DestructibleCollapsedMessage>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<DestructibleCollapsedMessage>(new DestructibleCollapsedMessage(null));
    }
  }
}
