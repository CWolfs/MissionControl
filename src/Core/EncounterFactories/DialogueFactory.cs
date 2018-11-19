using UnityEngine;
using System;

using BattleTech;

using MissionControl.RuntimeCast;

namespace MissionControl.EncounterFactories {
  public class DialogueFactory {
    private static GameObject CreateDialogLogicGameObject(GameObject parent, string name) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject dialogueGameLogicGo = new GameObject($"Dialogue_{name}");
      dialogueGameLogicGo.transform.parent = parent.transform;
      dialogueGameLogicGo.transform.localPosition = Vector3.zero;

      return dialogueGameLogicGo;
    }
    
    public static DialogueGameLogic CreateDialogLogic(GameObject parent, string name, string cameraTargetGuid) {
      GameObject dialogueGameLogicGo = CreateDialogLogicGameObject(parent, name);
      
      DialogueGameLogic dialogueGameLogic = dialogueGameLogicGo.AddComponent<DialogueGameLogic>();
      dialogueGameLogic.conversationContent = CreateConversationContent(cameraTargetGuid);

      return dialogueGameLogic;
    }

    /*
      Buckets seem to be a collection of conversation contents that contain only one conversation dialogue.
      They are selected at random. Good for a certain level fo variation but fairly shallow in depth.
      Buckets are loaded from /conversationBuckets and they take their individual entries from /conversations/dialogue_buckets
    */
    public static DialogueGameLogic CreateBucketDialogLogic(GameObject parent, string name, string dialogueBucketId) {
      GameObject dialogueGameLogicGo = CreateDialogLogicGameObject(parent, name);

      DialogueGameLogic dialogueGameLogic = dialogueGameLogicGo.AddComponent<DialogueGameLogic>();
      dialogueGameLogic.dialogueSource = DialogueSourceType.FromBucket;
      dialogueGameLogic.dialogBucketId = dialogueBucketId;

      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      combatState.DataManager.RequestResource(BattleTechResourceType.DialogBucketDef, dialogueBucketId, null);
      combatState.DataManager.ProcessRequests();

      return dialogueGameLogic;
    }

    public static ConversationContent CreateConversationContent(string cameraTargetGuid) {
      CastDef castDef = RuntimeCastFactory.CreateCast();
      DialogueContent dialogueContent1 = new DialogueContent(
        "Thanks for the assist on this matter, Commander. Let's wipe them out!",
        Color.blue, castDef.id, "Audio_Event_AmbushConvoy_EyesOn_3", 
        cameraTargetGuid, BattleTech.DialogCameraDistance.Medium, BattleTech.DialogCameraHeight.Default, -1
      );

      ConversationContent conversation = new ConversationContent("Conversation MC Test 1", new DialogueContent[] { dialogueContent1 });

      return conversation;
    }
  }
}