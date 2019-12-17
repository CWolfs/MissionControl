using UnityEngine;

using BattleTech;
using BattleTech.Designed;

using System;

using MissionControl.EncounterFactories;
using MissionControl.LogicComponents.Placers;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class ChunkTypeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private string name;
    private string type;
    private string subType;
    private bool controlledByContract;
    private string guid;
    private JObject position;
    private JArray children;

    public ChunkTypeBuilder(ContractTypeBuilder contractTypeBuilder, string name, string type, string subType, bool controlledByContract, string guid, JObject position, JArray children) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.name = name;
      this.type = type;
      this.controlledByContract = controlledByContract;
      this.subType = subType;
      this.guid = (guid == null) ? Guid.NewGuid().ToString() : guid;
      this.position = position;
      this.children = children;
    }

    public GameObject Build() {
      switch (subType) {
        case "PlayerLance": return BuildPlayerLanceChunk();
        case "DestroyWholeLance": return BuildDestroyWholeLanceChunk();
        case "EncounterBoundary": return BuildEncounterBoundary();
        case "Dialogue": return BuildDialogueChunk();
        case "Placement": return BuildPlacementChunk();
        default: break;
      }
      return null;
    }

    // TODO: If the below methods continue to be the same then I should extract them out into a generics method

    private GameObject BuildPlayerLanceChunk() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'PlayerLance' Chunk");
      PlayerLanceChunkGameLogic playerLanceChunkGameLogic = ChunkFactory.CreatePlayerLanceChunk(this.name, contractTypeBuilder.EncounterLayerGo.transform);
      playerLanceChunkGameLogic.encounterObjectGuid = this.guid;
      if (controlledByContract) playerLanceChunkGameLogic.startingStatus = EncounterObjectStatus.ControlledByContract;
      return playerLanceChunkGameLogic.gameObject;
    }

    private GameObject BuildDestroyWholeLanceChunk() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'DestroyWholeLance' Chunk");
      DestroyWholeLanceChunk destroyWholeLanceChunkGameLogic = ChunkFactory.CreateDestroyWholeLanceChunk(this.name, contractTypeBuilder.EncounterLayerGo.transform);
      destroyWholeLanceChunkGameLogic.encounterObjectGuid = this.guid;
      if (controlledByContract) destroyWholeLanceChunkGameLogic.startingStatus = EncounterObjectStatus.ControlledByContract;
      return destroyWholeLanceChunkGameLogic.gameObject;
    }

    private GameObject BuildEncounterBoundary() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'BuildEncounterBoundary' Chunk");
      EncounterBoundaryChunkGameLogic encounterBoundaryChunkLogic = ChunkFactory.CreateEncounterBondaryChunk(this.name, contractTypeBuilder.EncounterLayerGo.transform);
      encounterBoundaryChunkLogic.encounterObjectGuid = this.guid;
      if (controlledByContract) encounterBoundaryChunkLogic.startingStatus = EncounterObjectStatus.ControlledByContract;
      return encounterBoundaryChunkLogic.gameObject;
    }

    private GameObject BuildDialogueChunk() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'BuildDialogueChunk' Chunk");
      DialogueChunkGameLogic dialogueChunk = ChunkFactory.CreateDialogueChunk(this.name, contractTypeBuilder.EncounterLayerGo.transform);
      dialogueChunk.encounterObjectGuid = this.guid;
      if (controlledByContract) dialogueChunk.startingStatus = EncounterObjectStatus.ControlledByContract;
      return dialogueChunk.gameObject;
    }

    private GameObject BuildPlacementChunk() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'BuildPlacementChunk' Chunk");
      SwapPlacementChunkLogic swapPlacementChunk = ChunkFactory.CreateSwapPlacementChunk(this.name, contractTypeBuilder.EncounterLayerGo.transform);
      swapPlacementChunk.encounterObjectGuid = this.guid;
      if (controlledByContract) swapPlacementChunk.startingStatus = EncounterObjectStatus.ControlledByContract;
      return swapPlacementChunk.gameObject;
    }
  }
}