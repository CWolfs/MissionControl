using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.EncounterFactories;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class AddDialogueChunk : ChunkLogic {
    private string dialogueGuid;
    private string dialogChunkName;
    private string debugDescription;
    private string cameraTargetGuid;

    private bool fromDialogueBucket = false;
    private string dialogueBucketId;

    public AddDialogueChunk(string dialogueGuid, string dialogChunkName, string debugDescription, string cameraTargetGuid) {
      this.dialogueGuid = dialogueGuid;
      this.dialogChunkName = dialogChunkName;
      this.debugDescription = debugDescription;
      this.cameraTargetGuid = cameraTargetGuid;
    }

    public AddDialogueChunk(string dialogueGuid, string dialogChunkName, string debugDescription, string dialogueBucketId, string cameraTargetGuid) {
      this.dialogueGuid = dialogueGuid;
      this.dialogChunkName = dialogChunkName;
      this.debugDescription = debugDescription;
      this.fromDialogueBucket = true;
      this.dialogueBucketId = dialogueBucketId;
      this.cameraTargetGuid = cameraTargetGuid;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddDialogueChunk] Adding encounter structure");
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      DialogueChunkGameLogic dialogChunk = ChunkFactory.CreateDialogueChunk($"Chunk_Dialog_{dialogChunkName}");
      dialogChunk.encounterObjectGuid = System.Guid.NewGuid().ToString();
      dialogChunk.notes = debugDescription;

      DialogueGameLogic dialogueGameLogic;

      if (fromDialogueBucket) {
        dialogueGameLogic =  DialogueFactory.CreateBucketDialogLogic(dialogChunk.gameObject, dialogChunkName, dialogueBucketId);
      } else {
        dialogueGameLogic =  DialogueFactory.CreateDialogLogic(dialogChunk.gameObject, dialogChunkName, cameraTargetGuid);
      }

      dialogueGameLogic.encounterObjectGuid = dialogueGuid;
    }
  }
}