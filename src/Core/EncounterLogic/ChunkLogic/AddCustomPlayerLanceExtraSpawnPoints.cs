using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;
using MissionControl.EncounterFactories;

namespace MissionControl.Logic {
  public class AddCustomPlayerLanceExtraSpawnPoints : ChunkLogic {
    public AddCustomPlayerLanceExtraSpawnPoints() { }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddCustomPlayerLanceExtraSpawnPoints] Adding extra player lance spawn points to handle more player units");
      Contract contract = MissionControl.Instance.CurrentContract;
      GameObject playerSpawnGo = GetPlayerSpawn();
      IncreaseLanceSpawnPoints(playerSpawnGo);
    }

    private void IncreaseLanceSpawnPoints(GameObject playerSpawnGo) {
      SpawnableUnit[] lanceUnits = MissionControl.Instance.CurrentContract.Lances.GetLanceUnits(EncounterRules.PLAYER_TEAM_ID);
      List<GameObject> unitSpawnPoints = playerSpawnGo.FindAllContains("SpawnPoint");

      for (int i = unitSpawnPoints.Count; i < lanceUnits.Length; i++) {
        Vector3 randomLanceSpawn = unitSpawnPoints.GetRandom().transform.localPosition;
        Vector3 spawnPositon = SceneUtils.GetRandomPositionFromTarget(randomLanceSpawn, 24, 100);
        spawnPositon = spawnPositon.GetClosestHexLerpedPointOnGrid();
                      
        Main.Logger.Log($"[AddCustomPlayerLanceExtraSpawnPoints] Creating lance 'Player Lance' spawn point 'UnitSpawnPoint{i + 1}'");
        LanceSpawnerFactory.CreateUnitSpawnPoint(playerSpawnGo, $"UnitSpawnPoint{i + 1}", spawnPositon, Guid.NewGuid().ToString());
      }
    }
  }
}