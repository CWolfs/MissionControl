using UnityEngine;

using BattleTech;

using MissionControl.Messages;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class ContractTypeBuilder {
    public JObject ContractTypeBuild { get; set; }
    public GameObject EncounterLayerGo { get; set; }
    public string ContractTypeKey { get; set; } = "UNSET";

    private const string CHUNKS_ID = "Chunks";

    public ContractTypeBuilder(GameObject encounterLayerGo, JObject contractTypeBuild) {
      this.ContractTypeBuild = contractTypeBuild;
      this.EncounterLayerGo = encounterLayerGo;
      ContractTypeKey = contractTypeBuild["Key"].ToString();
    }

    public bool Build() {
      Main.LogDebug($"[ContractTypeBuild] Building '{ContractTypeKey}'");

      BuildChunks();

      return true;
    }

    private void BuildChunks() {
      if (ContractTypeBuild.ContainsKey(CHUNKS_ID)) {
        JArray chunksArray = (JArray)ContractTypeBuild[CHUNKS_ID];
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] There are '{chunksArray.Count} chunk(s) defined.'");
        foreach (JObject chunk in chunksArray.Children<JObject>()) {
          BuildChunk(chunk);
        }
      }
    }

    public void BuildChunk(JObject chunk) {
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Chunk is '{chunk["Name"]}'");
      string name = chunk["Name"].ToString();
      string type = chunk["Type"].ToString();
      string subType = chunk["SubType"].ToString();
      bool controlledByContract = (chunk.ContainsKey("ControlledByContract")) ? (bool)chunk["ControlledByContract"] : false;
      string guid = (chunk.ContainsKey("Guid")) ? chunk["Guid"].ToString() : null;
      JObject position = (JObject)chunk["Position"];
      JArray children = (JArray)chunk["Children"];

      ChunkTypeBuilder chunkTypeBuilder = new ChunkTypeBuilder(this, name, type, subType, controlledByContract, guid, position, children);
      GameObject chunkGo = chunkTypeBuilder.Build();
      if (chunkGo == null) {
        Main.Logger.LogError("[ContractTypeBuild.{ContractTypeKey}] Chunk creation failed. GameObject is null");
      }

      foreach (JObject child in children.Children<JObject>()) {
        BuildNode(chunkGo, child);
      }
    }

    public void BuildNode(GameObject parent, JObject child) {
      string type = child["Type"].ToString();
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Child type is '{type}'");

      NodeBuilder nodeBuilder = null;

      switch (type) {
        case "Spawner": nodeBuilder = new SpawnBuilder(this, parent, child); break;
        case "Objective": nodeBuilder = new ObjectiveBuilder(this, parent, child); break;
        case "Region": nodeBuilder = new RegionBuilder(this, parent, child); break;
        case "Dialogue": nodeBuilder = new DialogueBuilder(this, parent, child); break;
        case "SwapPlacement": nodeBuilder = new PlacementBuilder(this, parent, child); break;
        default: break;
      }

      if (nodeBuilder != null) {
        nodeBuilder.Build();
      } else {
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] No valid node was built for '{type}'");
      }
    }
  }
}