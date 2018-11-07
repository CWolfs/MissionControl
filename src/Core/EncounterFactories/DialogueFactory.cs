using UnityEngine;
using System;

using BattleTech;

namespace MissionControl.EncounterFactories {
  public class DialogueFactory {
    public static DialogueGameLogic CreateDialogLogic(GameObject parent, string name) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject dialogueGameLogicGo = new GameObject($"Dialogue_{name}");
      dialogueGameLogicGo.transform.parent = parent.transform;
      dialogueGameLogicGo.transform.localPosition = Vector3.zero;
      
      DialogueGameLogic dialogGameLogic = dialogueGameLogicGo.AddComponent<DialogueGameLogic>();
      dialogGameLogic.conversationContent = CreateTestConversationContent();

      return dialogGameLogic;
    }

    public static ConversationContent CreateTestConversationContent() {
      // DialogueContent dialogueContent1 = new DialogueContent("Hello World, Commander", Color.blue, "castDef_DariusDefault", "Audio_Event_AmbushConvoy_EyesOn_3", 
      //  "", BattleTech.DialogCameraDistance.Far, BattleTech.DialogCameraHeight.Default, -1);
       DialogueContent dialogueContent1 = new DialogueContent("Thanks for the assist on this matter, Commander. Let's wipe them out!", Color.blue, "castDef_SE01_AranoGuardsmanDefault", "Audio_Event_AmbushConvoy_EyesOn_3", 
        "", BattleTech.DialogCameraDistance.Medium, BattleTech.DialogCameraHeight.Default, -1);

      ConversationContent conversation = new ConversationContent("Conversation MC Test 1", new DialogueContent[] { dialogueContent1 });

      return conversation;
    }
  }
}