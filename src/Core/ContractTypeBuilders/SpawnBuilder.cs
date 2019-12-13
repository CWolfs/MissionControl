using UnityEngine;

using BattleTech;
using BattleTech.Designed;

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
    private string team;
    private string guid;
    private int spawnPoints;
    private List<string> spawnPointGuids;
    private string spawnType;

    public SpawnBuilder(ContractTypeBuilder contractTypeBuilder, GameObject parent, JObject spawner) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.parent = parent;
      this.name = spawner["Name"].ToString();
      this.subType = spawner["SubType"].ToString();
      this.team = spawner["Team"].ToString();
      this.guid = spawner["Guid"].ToString();
      this.spawnPoints = (int)spawner["SpawnPoints"];
      this.spawnPointGuids = spawner["SpawnPointGuids"].ToObject<List<string>>();
      this.spawnType = spawner["SpawnType"].ToString();
      this.position = spawner.ContainsKey("Position") ? (JObject)spawner["Position"] : null;
    }

    public override void Build() {
      SpawnUnitMethodType spawnMethodType = SpawnUnitMethodType.ViaLeopardDropship;
      switch (spawnType) {
        case "Leopard": spawnMethodType = SpawnUnitMethodType.ViaLeopardDropship; break;
        case "DropPod": spawnMethodType = SpawnUnitMethodType.DropPod; break;
        case "Instant": spawnMethodType = SpawnUnitMethodType.InstantlyAtSpawnPoint; break;
        default: Main.LogDebug($"[SpawnBuilder.{contractTypeBuilder.ContractTypeKey}] No support for spawnType '{spawnType}'. Check for spelling mistakes."); break;
      }

      string teamId = EncounterRules.PLAYER_TEAM_ID;
      switch (team) {
        case "Player1": {
          teamId = EncounterRules.PLAYER_TEAM_ID;
          PlayerLanceSpawnerGameLogic playerLanceSpawnerGameLogic = LanceSpawnerFactory.CreatePlayerLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids, true);
          if (position != null) SetPosition(playerLanceSpawnerGameLogic.gameObject, position);
          break;
        }
        case "Target": {
          teamId = EncounterRules.TARGET_TEAM_ID;
          LanceSpawnerGameLogic lanceSpawnerGameLogic = LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          if (position != null) SetPosition(lanceSpawnerGameLogic.gameObject, position);
          break;
        }
        case "TargetAlly": {
          teamId = EncounterRules.TARGETS_ALLY_TEAM_ID;
          LanceSpawnerGameLogic lanceSpawnerGameLogic = LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          if (position != null) SetPosition(lanceSpawnerGameLogic.gameObject, position);
          break;
        }
        case "Employer": {
          teamId = EncounterRules.EMPLOYER_TEAM_ID;
          LanceSpawnerGameLogic lanceSpawnerGameLogic = LanceSpawnerFactory.CreateLanceSpawner(parent, name, guid, teamId, true, spawnMethodType, spawnPointGuids);
          if (position != null) SetPosition(lanceSpawnerGameLogic.gameObject, position);
          break;
        }
        default: Main.LogDebug($"[SpawnBuilder.{contractTypeBuilder.ContractTypeKey}] No support for team '{team}'. Check for spelling mistakes."); break;
      }
    }
  }
}