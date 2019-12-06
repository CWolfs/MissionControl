using UnityEngine;

using MissionControl.Rules;
using MissionControl.EncounterFactories;

using BattleTech;

using System.Collections.Generic;

using Newtonsoft.Json.Linq;

namespace MissionControl.Logic {
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

    private void BuildChunk(JObject chunk) {
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Chunk is '{chunk["Name"]}'");
      string chunkName = chunk["Name"].ToString();
      string chunkType = chunk["Type"].ToString();
      JObject placement = (JObject)chunk["Placement"];
      JArray children = (JArray)chunk["Children"];

      ChunkTypeBuilder chunkTypeBuilder = new ChunkTypeBuilder(this, chunkName, chunkType, placement, children);
      GameObject chunkGo = chunkTypeBuilder.Build();
      if (chunkGo == null) {
        Main.Logger.LogError("[ContractTypeBuild.{ContractTypeKey}] Chunk creation failed. GameObject is null");
      }

      foreach (JObject child in children.Children<JObject>()) {
        string type = child["Type"].ToString();
        Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Child type is '{type}'");

        switch (type) {
          case "Spawner": BuildSpawner(chunkGo, child); break;
          default: break;
        }
      }
    }

    private void BuildSpawner(GameObject parent, JObject spawner) {
      string name = spawner["Name"].ToString();
      string type = spawner["Type"].ToString();
      string team = spawner["Team"].ToString();
      string guid = spawner["Guid"].ToString();
      int spawnPoints = (int)spawner["SpawnPoints"];
      List<string> spawnPointGuids = spawner["SpawnPointGuids"].ToObject<List<string>>();
      string spawnType = spawner["SpawnType"].ToString();

      string teamId = EncounterRules.PLAYER_TEAM_ID;
      switch (team) {
        case "Target": teamId = EncounterRules.TARGET_TEAM_ID; break;
        case "TargetAlly": teamId = EncounterRules.TARGETS_ALLY_TEAM_ID; break;
        case "Employer": teamId = EncounterRules.EMPLOYER_TEAM_ID; break;
      }

      SpawnUnitMethodType spawnMethodType = SpawnUnitMethodType.ViaLeopardDropship;
      switch (spawnType) {
        case "DropPod": spawnMethodType = SpawnUnitMethodType.DropPod; break;
        case "Instant": spawnMethodType = SpawnUnitMethodType.InstantlyAtSpawnPoint; break;
      }

      LanceSpawnerFactory.CreatePlayerLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
    }
  }
}