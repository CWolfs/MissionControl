using UnityEngine;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using BattleTech;
using BattleTech.Framework;

using MissionControl.RuntimeCast;
using MissionControl.LogicComponents.Activators;

namespace MissionControl.EncounterFactories {
  public class DialogueFactory {
    private static GameObject CreateDialogLogicGameObject(GameObject parent, string name) {
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      name = (!name.StartsWith("Dialogue_")) ? $"Dialogue_{name}" : name;
      GameObject dialogueGameLogicGo = new GameObject(name);
      dialogueGameLogicGo.transform.parent = parent.transform;
      dialogueGameLogicGo.transform.localPosition = Vector3.zero;

      return dialogueGameLogicGo;
    }

    public static DialogueActivator CreateDialogueActivator(GameObject parent, string guid) {
      DialogueActivator dialogueActivator = parent.AddComponent<DialogueActivator>();
      dialogueActivator.encounterObjectGuid = Guid.NewGuid().ToString();
      dialogueActivator.dialogueGuid = guid;
      return dialogueActivator;
    }

    public static DialogueGameLogic CreateDialogLogic(GameObject parent, string name, string cameraTargetGuid, string presetDialogue = null, CastDef castDef = null) {
      GameObject dialogueGameLogicGo = CreateDialogLogicGameObject(parent, name);

      DialogueGameLogic dialogueGameLogic = dialogueGameLogicGo.AddComponent<DialogueGameLogic>();
      if (presetDialogue == null) {
        presetDialogue = DataManager.Instance.GetRandomDialogue("AllyDrop",
          MissionControl.Instance.CurrentContractType, MissionControl.Instance.EncounterRulesName);
      }
      dialogueGameLogic.conversationContent = CreateConversationContent(presetDialogue, cameraTargetGuid, castDef);

      return dialogueGameLogic;
    }

    public static DialogueGameLogic CreateDialogLogic(GameObject parent, string name, string guid, bool showOnlyOnce) {
      GameObject dialogueGameLogicGo = CreateDialogLogicGameObject(parent, name);
      DialogueGameLogic dialogueGameLogic = dialogueGameLogicGo.AddComponent<DialogueGameLogic>();
      dialogueGameLogic.encounterObjectGuid = guid;
      dialogueGameLogic.showOnlyOnce = showOnlyOnce;
      return dialogueGameLogic;
    }

    public static DialogueGameLogic CreateDialogLogic(GameObject parent, string name, DialogueOverride dialogueOverride) {
      GameObject dialogueGameLogicGo = CreateDialogLogicGameObject(parent, name);
      Dictionary<string, EncounterObjectGameLogic> encounterObjects = new Dictionary<string, EncounterObjectGameLogic>();
      MissionControl.Instance.EncounterLayerData.BuildEncounterObjectDictionary(encounterObjects);

      DialogueGameLogic dialogueGameLogic = dialogueGameLogicGo.AddComponent<DialogueGameLogic>();
      dialogueGameLogic.ApplyContractOverride(dialogueOverride, encounterObjects);

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
      DataManager.Instance.RequestResourcesAndProcess(BattleTechResourceType.DialogBucketDef, dialogueBucketId);

      return dialogueGameLogic;
    }

    public static ConversationContent CreateConversationContent(string presetDialogue, string cameraTargetGuid, CastDef cast = null) {
      CastDef castDef = (cast == null) ? RuntimeCastFactory.CreateCast() : cast;

      if (MissionControl.Instance.IsSkirmish()) presetDialogue = Regex.Replace(presetDialogue, "{COMMANDER\\..+}", "Commander");

      DialogueContent dialogueContent1 = new DialogueContent(
        presetDialogue, Color.white, castDef.id, "",
        cameraTargetGuid, BattleTech.DialogCameraDistance.Medium, BattleTech.DialogCameraHeight.Default, -1
      );

      ConversationContent conversation = new ConversationContent("Conversation MC Test 1", new DialogueContent[] { dialogueContent1 });

      return conversation;
    }
  }
}