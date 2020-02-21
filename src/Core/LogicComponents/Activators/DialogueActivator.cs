using UnityEngine;

using BattleTech;

using HBS.Util;

using MissionControl.Data;

namespace MissionControl.LogicComponents.Activators {
  public class DialogueActivator : EncounterObjectGameLogic, ExecutableGameLogic {

    [SerializeField]
    public string dialogueGuid { get; set; }

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
          ActivateDialogue();
        }
      }
    }

    private void ActivateDialogue() {
      Main.LogDebug($"[DialogueActivator.ActivateDialogue]) Activating dialogue...");
      EncounterObjectGameLogic dialogue = MissionControl.Instance.EncounterLayerData.gameObject.GetEncounterObjectGameLogic(dialogueGuid);

      if (dialogue is DialogueGameLogic) {
        Main.LogDebug($"[DialogueActivator.ActivateDialogue]) Activating dialogue for '{dialogueGuid}:{dialogue.gameObject.name}'");
        ((DialogueGameLogic)dialogue).TriggerDialogue(true);
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
