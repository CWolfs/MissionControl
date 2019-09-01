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
    private string presetDialog;
    private CastDef castDef;

    private bool fromDialogueBucket = false;
    private string dialogueBucketId;

    private DialogueOverride dialogueOverride;

    public AddDialogueChunk(string dialogueGuid, string dialogChunkName, string debugDescription, string cameraTargetGuid, bool usePresetDialog = true, string presetDialog = null, CastDef castDef = null) {
      this.dialogueGuid = dialogueGuid;
      this.dialogChunkName = dialogChunkName;
      this.debugDescription = debugDescription;
      this.cameraTargetGuid = cameraTargetGuid;
      if (usePresetDialog) this.presetDialog = presetDialog;
      this.castDef = castDef;
    }

    public AddDialogueChunk(string dialogueGuid, string dialogChunkName, string debugDescription, string dialogueBucketId, string cameraTargetGuid) {
      this.dialogueGuid = dialogueGuid;
      this.dialogChunkName = dialogChunkName;
      this.debugDescription = debugDescription;
      this.fromDialogueBucket = true;
      this.dialogueBucketId = dialogueBucketId;
      this.cameraTargetGuid = cameraTargetGuid;
    }

    public AddDialogueChunk(string dialogueGuid, string dialogChunkName, string debugDescription, DialogueOverride dialogueOverride) {
      this.dialogueGuid = dialogueGuid;
      this.dialogChunkName = dialogChunkName;
      this.debugDescription = debugDescription;
      this.dialogueOverride = dialogueOverride;
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
      } else if (dialogueOverride != null) {
        dialogueGameLogic = DialogueFactory.CreateDialogLogic(dialogChunk.gameObject, dialogChunkName, dialogueOverride);
      } else {
        dialogueGameLogic =  DialogueFactory.CreateDialogLogic(dialogChunk.gameObject, dialogChunkName, cameraTargetGuid, presetDialog, castDef);
      }

      dialogueGameLogic.encounterObjectGuid = dialogueGuid;
    }
  }
}