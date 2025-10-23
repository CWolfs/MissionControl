using UnityEngine;

using BattleTech;

using HBS.Util;

using MissionControl.Data;

namespace MissionControl.LogicComponents.Activators {
  public class DialogueActivator : EncounterObjectGameLogic, ExecutableGameLogic {

    [SerializeField]
    public string DialogueGuid { get; set; }

    [SerializeField]
    public bool IsInterrupt { get; set; } = true;

    [SerializeField]
    public EncounterChunkGameLogic Chunk { get; set; }

    [SerializeField]
    public bool HasActivated { get; set; } = false;

    public override TaggedObjectType Type {
      get {
        return (TaggedObjectType)MCTaggedObjectType.ActivateDialogue;
      }
    }

    void Start() {
      Chunk = this.GetComponent<EncounterChunkGameLogic>();
    }

    void Update() {
      if (Chunk != null && !HasActivated) {
        if (Chunk.GetState() == EncounterObjectStatus.Active) {
          HasActivated = true;
          ActivateDialogue();
        }
      }
    }

    private void ActivateDialogue() {
      Main.LogDebug($"[DialogueActivator.ActivateDialogue]) Activating dialogue...");
      EncounterObjectGameLogic dialogue = MissionControl.Instance.EncounterLayerData.gameObject.GetEncounterObjectGameLogic(DialogueGuid);

      if (dialogue is DialogueGameLogic) {
        Main.LogDebug($"[DialogueActivator.ActivateDialogue]) Activating dialogue for '{DialogueGuid}:{dialogue.gameObject.name}' and isInterrupt={IsInterrupt}");
        ((DialogueGameLogic)dialogue).TriggerDialogue(IsInterrupt);
      }
    }

    public override void FromJSON(string json) {
      JSONSerializationUtility.FromJSON<DialogueActivator>(this, json);
    }

    public override string GenerateJSONTemplate() {
      return JSONSerializationUtility.ToJSON<DialogueActivator>(new DialogueActivator());
    }

    public override string ToJSON() {
      return JSONSerializationUtility.ToJSON<DialogueActivator>(this);
    }

    public void Execute() {
      ActivateDialogue();
    }
  }
}
