using UnityEngine;

using BattleTech;
using BattleTech.Designed;

using HBS.Collections;

using System;
using System.Collections.Generic;

using MissionControl.Rules;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class SpawnBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private GameObject parent;
    private string name;
    private string subType;
    private JObject position;
    private JObject rotation;
    private string team;
    private string guid;
    private List<string> spawnPointGuids;
    private bool preciseSpawnPoints;
    private JObject mountOnTargets;
    private JObject spawnPointPositions;
    private JObject spawnPointRotations;
    private string spawnType;
    private JArray aiOrdersArray;
    private List<AIOrderBox> orders;
    private bool alertLanceOnSpawn;
    private int defaultDetectionRange;
    private List<string> tags;

    public SpawnBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject spawner) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.parent = parent;
      this.name = spawner["Name"].ToString();
      this.subType = spawner["SubType"].ToString();
      this.team = spawner["Team"].ToString();
      this.guid = spawner["Guid"].ToString();
      this.spawnPointGuids = spawner["SpawnPointGuids"].ToObject<List<string>>();
      this.preciseSpawnPoints = spawner.ContainsKey("PreciseSpawnPoints") ? (bool)spawner["PreciseSpawnPoints"] : false;
      this.mountOnTargets = spawner.ContainsKey("MountOn") ? (JObject)spawner["MountOn"] : null;
      this.spawnPointPositions = spawner.ContainsKey("SpawnPointPositions") ? (JObject)spawner["SpawnPointPositions"] : null;
      this.spawnPointRotations = spawner.ContainsKey("SpawnPointRotations") ? (JObject)spawner["SpawnPointRotations"] : null;
      this.spawnType = spawner["SpawnType"].ToString();
      this.position = spawner.ContainsKey("Position") ? (JObject)spawner["Position"] : null;
      this.rotation = spawner.ContainsKey("Rotation") ? (JObject)spawner["Rotation"] : null;
      this.aiOrdersArray = spawner.ContainsKey("AI") ? (JArray)spawner["AI"] : null;
      this.alertLanceOnSpawn = spawner.ContainsKey("AlertLanceOnSpawn") ? (bool)spawner["AlertLanceOnSpawn"] : false;
      this.defaultDetectionRange = spawner.ContainsKey("DefaultDetectionRange") ? (int)spawner["DefaultDetectionRange"] : 200;
      this.tags = spawner.ContainsKey("Tags") ? spawner["Tags"].ToObject<List<string>>() : null;

      if (this.aiOrdersArray != null) {
        AiOrderBuilder orderBuilder = new AiOrderBuilder(contractTypeBuilder, aiOrdersArray, name);
        orders = orderBuilder.Build();
      }
    }

    public override void Build() {
      SpawnUnitMethodType spawnMethodType = SpawnUnitMethodType.ViaLeopardDropship;
      switch (spawnType) {
        case "Leopard": spawnMethodType = SpawnUnitMethodType.ViaLeopardDropship; break;
        case "DropPod": spawnMethodType = SpawnUnitMethodType.DropPod; break;
        case "Instant": spawnMethodType = SpawnUnitMethodType.InstantlyAtSpawnPoint; break;
        default: Main.LogDebug($"[SpawnBuilder.{contractTypeBuilder.ContractTypeKey}] No support for spawnType '{spawnType}'. Check for spelling mistakes."); break;
      }

      LanceSpawnerGameLogic spawnerGameLogic = null;

      string teamId = TeamUtils.PLAYER_TEAM_ID;
      switch (team) {
        case "Player1": {
          teamId = TeamUtils.PLAYER_TEAM_ID;
          spawnerGameLogic = LanceSpawnerFactory.CreatePlayerLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids, true);
          break;
        }
        case "Target": {
          teamId = TeamUtils.TARGET_TEAM_ID;
          spawnerGameLogic = LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          spawnerGameLogic.alertLanceOnSpawn = this.alertLanceOnSpawn;
          if (orders != null) spawnerGameLogic.aiOrderList.contentsBox = orders;
          break;
        }
        case "TargetAlly": {
          teamId = TeamUtils.TARGETS_ALLY_TEAM_ID;
          spawnerGameLogic = LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          spawnerGameLogic.alertLanceOnSpawn = this.alertLanceOnSpawn;
          if (orders != null) spawnerGameLogic.aiOrderList.contentsBox = orders;
          break;
        }
        case "Employer": {
          teamId = TeamUtils.EMPLOYER_TEAM_ID;
          spawnerGameLogic = LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          spawnerGameLogic.alertLanceOnSpawn = this.alertLanceOnSpawn;
          if (orders != null) spawnerGameLogic.aiOrderList.contentsBox = orders;
          break;
        }
        case "NeutralToAll": {
          teamId = TeamUtils.NEUTRAL_TO_ALL_TEAM_ID;
          spawnerGameLogic = LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          spawnerGameLogic.alertLanceOnSpawn = this.alertLanceOnSpawn;
          if (orders != null) spawnerGameLogic.aiOrderList.contentsBox = orders;
          break;
        }
        case "HostileToAll": {
          teamId = TeamUtils.HOSTILE_TO_ALL_TEAM_ID;
          spawnerGameLogic = LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          spawnerGameLogic.alertLanceOnSpawn = this.alertLanceOnSpawn;
          if (orders != null) spawnerGameLogic.aiOrderList.contentsBox = orders;
          break;
        }
        default: Main.Logger.LogError($"[SpawnBuilder.{contractTypeBuilder.ContractTypeKey}] No support for team '{team}'. Check for spelling mistakes."); break;
      }

      if (this.position != null) SetPosition(spawnerGameLogic.gameObject, this.position);
      if (this.rotation != null) SetRotation(spawnerGameLogic.gameObject, this.rotation);
      if (this.mountOnTargets != null) SetMountOnPositions(spawnerGameLogic, this.mountOnTargets);
      if (this.spawnPointPositions != null) SetSpawnPointPositions(spawnerGameLogic);
      if (this.spawnPointRotations != null) SetSpawnPointRotations(spawnerGameLogic);
      if (this.defaultDetectionRange > 0) SetDefaultDetectionRange(spawnerGameLogic, this.defaultDetectionRange);
      if (this.tags != null) spawnerGameLogic.encounterTags.AddRange(this.tags);
    }

    private void SetMountOnPositions(LanceSpawnerGameLogic spawnerGameLogic, JObject mountOnTargets) {
      UnitSpawnPointGameLogic[] unitSpawnPoints = spawnerGameLogic.unitSpawnPointGameLogicList;
      foreach (UnitSpawnPointGameLogic spawnPoint in unitSpawnPoints) {
        string targetPath = mountOnTargets.ContainsKey(spawnPoint.encounterObjectGuid) ? mountOnTargets[spawnPoint.encounterObjectGuid].ToString() : null;
        if (targetPath != null) SetMountOnPosition(spawnPoint.gameObject, targetPath);
      }
    }

    private void SetSpawnPointPositions(LanceSpawnerGameLogic spawnerGameLogic) {
      UnitSpawnPointGameLogic[] unitSpawnPoints = spawnerGameLogic.unitSpawnPointGameLogicList;
      foreach (UnitSpawnPointGameLogic spawnPoint in unitSpawnPoints) {
        JObject position = spawnPointPositions.ContainsKey(spawnPoint.encounterObjectGuid) ? (JObject)spawnPointPositions[spawnPoint.encounterObjectGuid] : null;
        if (position != null) SetPosition(spawnPoint.gameObject, position, exactPosition: this.preciseSpawnPoints);
      }
    }

    private void SetSpawnPointRotations(LanceSpawnerGameLogic spawnerGameLogic) {
      UnitSpawnPointGameLogic[] unitSpawnPoints = spawnerGameLogic.unitSpawnPointGameLogicList;
      foreach (UnitSpawnPointGameLogic spawnPoint in unitSpawnPoints) {
        JObject rotation = spawnPointRotations.ContainsKey(spawnPoint.encounterObjectGuid) ? (JObject)spawnPointRotations[spawnPoint.encounterObjectGuid] : null;
        if (rotation != null) SetRotation(spawnPoint.gameObject, rotation);
      }
    }

    private void SetDefaultDetectionRange(LanceSpawnerGameLogic spawnerGameLogic, int defaultDetectionRange) {
      UnitSpawnPointGameLogic[] unitSpawnPoints = spawnerGameLogic.unitSpawnPointGameLogicList;
      foreach (UnitSpawnPointGameLogic spawnPoint in unitSpawnPoints) {
        spawnPoint.defaultDetectionRange = defaultDetectionRange;
      }
    }
  }
}