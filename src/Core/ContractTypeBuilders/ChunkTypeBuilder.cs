using UnityEngine;

using BattleTech;
using BattleTech.Designed;

using System;

using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class ChunkTypeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private string name;
    private string type;
    private string subType;
    private JObject position;
    private JArray children;

    public ChunkTypeBuilder(ContractTypeBuilder contractTypeBuilder, string name, string type, string subType, JObject position, JArray children) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.name = name;
      this.type = type;
      this.subType = subType;
      this.position = position;
      this.children = children;
    }

    public GameObject Build() {
      switch (subType) {
        case "PlayerLance": return BuildPlayerLanceChunk();
        case "DestroyWholeLance": return BuildDestroyWholeLanceChunk();
        case "EncounterBoundary": return BuildEncounterBoundary();
        case "Dialogue": return BuildDialogueChunk();
        default: break;
      }
      return null;
    }

    private GameObject BuildPlayerLanceChunk() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'PlayerLance' Chunk");
      PlayerLanceChunkGameLogic playerLanceChunkGameLogic = ChunkFactory.CreatePlayerLanceChunk(this.name, contractTypeBuilder.EncounterLayerGo.transform);
      playerLanceChunkGameLogic.encounterObjectGuid = Guid.NewGuid().ToString();
      return playerLanceChunkGameLogic.gameObject;
    }

    private GameObject BuildDestroyWholeLanceChunk() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'DestroyWholeLance' Chunk");
      DestroyWholeLanceChunk destroyWholeLanceChunkGameLogic = ChunkFactory.CreateDestroyWholeLanceChunk(this.name, contractTypeBuilder.EncounterLayerGo.transform);
      destroyWholeLanceChunkGameLogic.encounterObjectGuid = Guid.NewGuid().ToString();
      return destroyWholeLanceChunkGameLogic.gameObject;
    }

    private GameObject BuildEncounterBoundary() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'BuildEncounterBoundary' Chunk");
      EncounterBoundaryChunkGameLogic encounterBoundaryChunkLogic = ChunkFactory.CreateEncounterBondaryChunk(this.name, contractTypeBuilder.EncounterLayerGo.transform);
      encounterBoundaryChunkLogic.encounterObjectGuid = Guid.NewGuid().ToString();
      return encounterBoundaryChunkLogic.gameObject;
    }

    private GameObject BuildDialogueChunk() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'BuildDialogueChunk' Chunk");
      DialogueChunkGameLogic dialogueChunk = ChunkFactory.CreateDialogueChunk(this.name, contractTypeBuilder.EncounterLayerGo.transform);
      dialogueChunk.encounterObjectGuid = Guid.NewGuid().ToString();
      return dialogueChunk.gameObject;
    }
  }
}