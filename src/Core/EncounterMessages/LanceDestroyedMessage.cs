using System;
using HBS.Util;

namespace MissionControl.Messages {
  public class LanceDestroyedMessage : EncounterObjectMessage {

    public override MessageCenterMessageType MessageType {
      get {
        return (MessageCenterMessageType)MessageTypes.OnLanceDestroyed;
      }
    }

    public string SpawnerGuid {
      get {
        return base.actingObjectGuid;
      }
    }


    public string LanceSpawnedGuid {
      get {
        return base.affectedObjectGuid;
      }
    }

    public LanceDestroyedMessage(string spawnerGuid, string destroyedLanceGuid) : base(spawnerGuid, destroyedLanceGuid) { }

    public override void LoadComplete() {
      base.LoadComplete();
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<LanceDestroyedMessage>(this);
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<LanceDestroyedMessage>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<LanceDestroyedMessage>(new LanceDestroyedMessage(string.Empty, string.Empty));
    }
  }
}
