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
        string activationKey = GetActivationKey();
        if (MissionControl.Instance.GetGameLogicData(activationKey) == "true") {
          HasActivated = true;
          return;
        }

        if (Chunk.GetState() == EncounterObjectStatus.Active) {
          HasActivated = true;
          MissionControl.Instance.SetGameLogicData(activationKey, "true");
          ActivateDialogue();
        }
      }
    }

    private string GetActivationKey() {
      return $"DIALOGUE_ACTIVATOR_{Chunk.encounterObjectGuid}_{DialogueGuid}";
    }

    private void ActivateDialogue() {
      Main.LogDebug($"[DialogueActivator.ActivateDialogue]) Activating dialogue...");
      EncounterObjectGameLogic logic = MissionControl.Instance.EncounterLayerData.gameObject.GetEncounterObjectGameLogic(DialogueGuid);

      if (logic is DialogueGameLogic dialogue) {
        Main.LogDebug($"[DialogueActivator.ActivateDialogue]) Activating dialogue for '{DialogueGuid}:{dialogue.gameObject.name}' and isInterrupt={IsInterrupt}");
        dialogue.TriggerDialogue(IsInterrupt);
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
