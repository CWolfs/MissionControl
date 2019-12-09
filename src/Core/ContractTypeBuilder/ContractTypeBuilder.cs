using UnityEngine;

using MissionControl.Rules;
using MissionControl.Logic;
using MissionControl.EncounterFactories;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using Harmony;

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

    public void BuildChunk(JObject chunk) {
      Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] Chunk is '{chunk["Name"]}'");
      string name = chunk["Name"].ToString();
      string type = chunk["Type"].ToString();
      string subType = chunk["SubType"].ToString();
      JObject position = (JObject)chunk["Position"];
      JArray children = (JArray)chunk["Children"];

      ChunkTypeBuilder chunkTypeBuilder = new ChunkTypeBuilder(this, name, type, subType, position, children);
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

      switch (type) {
        case "Spawner": BuildSpawner(parent, child); break;
        case "Objective": BuildObjective(parent, child); break;
        default: break;
      }
    }

    private void BuildSpawner(GameObject parent, JObject spawner) {
      string name = spawner["Name"].ToString();
      string subType = spawner["SubType"].ToString();
      string team = spawner["Team"].ToString();
      string guid = spawner["Guid"].ToString();
      int spawnPoints = (int)spawner["SpawnPoints"];
      List<string> spawnPointGuids = spawner["SpawnPointGuids"].ToObject<List<string>>();
      string spawnType = spawner["SpawnType"].ToString();

      SpawnUnitMethodType spawnMethodType = SpawnUnitMethodType.ViaLeopardDropship;
      switch (spawnType) {
        case "Leopard": spawnMethodType = SpawnUnitMethodType.ViaLeopardDropship; break;
        case "DropPod": spawnMethodType = SpawnUnitMethodType.DropPod; break;
        case "Instant": spawnMethodType = SpawnUnitMethodType.InstantlyAtSpawnPoint; break;
        default: Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] No support for spawnType '{spawnType}'. Check for spelling mistakes."); break;
      }

      string teamId = EncounterRules.PLAYER_TEAM_ID;
      switch (team) {
        case "Player1": {
          teamId = EncounterRules.PLAYER_TEAM_ID;
          LanceSpawnerFactory.CreatePlayerLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          break;
        }
        case "Target": {
          teamId = EncounterRules.TARGET_TEAM_ID;
          LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          break;
        }
        case "TargetAlly": {
          teamId = EncounterRules.TARGETS_ALLY_TEAM_ID;
          LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          break;
        }
        case "Employer": {
          teamId = EncounterRules.EMPLOYER_TEAM_ID;
          LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          break;
        }
        default: Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] No support for team '{team}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildObjective(GameObject parent, JObject objective) {
      string name = objective["Name"].ToString();
      string subType = objective["SubType"].ToString();
      string guid = objective["Guid"].ToString();
      bool isPrimaryObjectve = (bool)objective["IsPrimaryObjective"];
      string label = objective["Label"].ToString();
      int priority = (int)objective["Priority"];
      string contractObjectiveGuid = objective["ContractObjectiveGuid"].ToString();

      switch (subType) {
        case "DestroyLance": BuildDestroyWholeLanceObjective(parent, objective, name, label, guid, isPrimaryObjectve, priority, contractObjectiveGuid); break;
        default: Main.LogDebug($"[ContractTypeBuild.{ContractTypeKey}] No support for sub-type '{subType}'. Check for spelling mistakes."); break;
      }
    }

    private void BuildDestroyWholeLanceObjective(GameObject parent, JObject objective, string name, string label, string guid,
      bool isPrimaryObjectve, int priority, string contractObjectiveGuid) {

      DestroyWholeLanceChunk destroyWholeLanceChunk = parent.GetComponent<DestroyWholeLanceChunk>();
      string lanceToDestroyGuid = objective["LanceToDestroyGuid"].ToString();
      bool showProgress = true;
      bool displayToUser = true;

      LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef();
      lanceSpawnerRef.EncounterObjectGuid = lanceToDestroyGuid;

      DestroyLanceObjective objectiveLogic = ObjectiveFactory.CreateDestroyLanceObjective(
        guid,
        parent,
        lanceSpawnerRef,
        lanceToDestroyGuid,
        label,
        showProgress,
        ChunkLogic.ProgressFormat.PERCENTAGE_COMPLETE,
        "The primary objective to destroy the enemy lance",
        priority,
        displayToUser,
        ObjectiveMark.AttackTarget,
        contractObjectiveGuid
      );

      if (isPrimaryObjectve) {
        AccessTools.Field(typeof(ObjectiveGameLogic), "primary").SetValue(objectiveLogic, true);
      } else {
        AccessTools.Field(typeof(ObjectiveGameLogic), "primary").SetValue(objectiveLogic, false);
      }

      DestroyLanceObjectiveRef destroyLanceObjectiveRef = new DestroyLanceObjectiveRef();
      destroyLanceObjectiveRef.encounterObject = objectiveLogic;

      destroyWholeLanceChunk.lanceSpawner = lanceSpawnerRef;
      destroyWholeLanceChunk.destroyObjective = destroyLanceObjectiveRef;
    }
  }
}