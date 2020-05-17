using UnityEngine;

using BattleTech;

using System.Collections.Generic;

using HBS.Util;

using MissionControl.Data;

namespace MissionControl.LogicComponents.Activators {
  public class SetChunkStateAtRandomActivator : EncounterObjectGameLogic, ExecutableGameLogic {

    [SerializeField]
    public List<string> ChunkGuids { get; set; } = new List<string>();

    [SerializeField]
    public EncounterObjectStatus State { get; set; }

    [SerializeField]
    public EncounterChunkGameLogic chunk { get; set; }

    [SerializeField]
    public bool HasActivated { get; set; } = false;

    public override TaggedObjectType Type {
      get {
        return (TaggedObjectType)MCTaggedObjectType.ActivateDialogue;
      }
    }

    void Start() {
      chunk = this.GetComponent<EncounterChunkGameLogic>();
    }

    void Update() {
      if (chunk != null && !HasActivated) {
        if (chunk.GetState() == EncounterObjectStatus.Active) {
          HasActivated = true;
          SetChunkState(ChunkGuids.GetRandom());
        }
      }
    }

    private void SetChunkState(string guid) {
      Main.LogDebug($"[SetChunkStateAtRandomActivator]) Setting chunk state");
      EncounterObjectGameLogic chunk = MissionControl.Instance.EncounterLayerData.gameObject.GetEncounterObjectGameLogic(guid);

      if (chunk is EncounterChunkGameLogic) {
        Main.LogDebug($"[SetChunkStateAtRandomActivator]) Setting chunk state for '{guid}:{chunk.gameObject.name}'");
        ((EncounterObjectGameLogic)chunk).SetState(State);
      }
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<SetChunkStateAtRandomActivator>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<SetChunkStateAtRandomActivator>(new SetChunkStateAtRandomActivator());
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<SetChunkStateAtRandomActivator>(this);
    }

    public void Execute() {
      SetChunkState(ChunkGuids.GetRandom());
    }
  }
}
