using UnityEngine;

using BattleTech;

using System;

using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.Logic {
  public class ChunkTypeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private string name;
    private string type;
    private JObject placement;
    private JArray children;

    public ChunkTypeBuilder(ContractTypeBuilder contractTypeBuilder, string name, string type, JObject placement, JArray children) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.name = name;
      this.type = type;
      this.placement = placement;
      this.children = children;
    }

    public GameObject Build() {
      switch (type) {
        case "PlayerLance": return BuildPlayerLanceChunk();
        default: break;
      }
      return null;
    }

    private GameObject BuildPlayerLanceChunk() {
      Main.LogDebug($"[ChunkTypeBuilder.{contractTypeBuilder.ContractTypeKey}] Building 'PlayerLance' Chunk");
      PlayerLanceChunkGameLogic playerLanceChunkGameLogic = ChunkFactory.CreatePlayerLanceChunk("Chunk_PlayerLance", contractTypeBuilder.EncounterLayerGo.transform);
      playerLanceChunkGameLogic.encounterObjectGuid = Guid.NewGuid().ToString();
      return playerLanceChunkGameLogic.gameObject;
    }
  }
}