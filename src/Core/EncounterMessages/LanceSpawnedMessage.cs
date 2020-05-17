using System;
using HBS.Util;

namespace MissionControl.Messages {
  public class LanceSpawnedMessage : EncounterObjectMessage {

    public override MessageCenterMessageType MessageType {
      get {
        return (MessageCenterMessageType)MessageTypes.OnLanceSpawned;
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

    public LanceSpawnedMessage(string spawnerGuid, string spawnedLanceGuid) : base(spawnerGuid, spawnedLanceGuid) { }

    public override void LoadComplete() {
      base.LoadComplete();
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<LanceSpawnedMessage>(this);
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<LanceSpawnedMessage>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<LanceSpawnedMessage>(new LanceSpawnedMessage(string.Empty, string.Empty));
    }
  }
}
