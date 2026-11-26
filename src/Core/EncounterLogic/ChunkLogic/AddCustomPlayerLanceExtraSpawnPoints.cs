using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Rules;
using MissionControl.EncounterFactories;

namespace MissionControl.Logic {
  public class AddCustomPlayerLanceExtraSpawnPoints : ChunkLogic {
    private EncounterRules encounterRules;
    private LogicState state;
    private List<string[]> spawnKeys = new List<string[]>();

    public AddCustomPlayerLanceExtraSpawnPoints() { }

    public AddCustomPlayerLanceExtraSpawnPoints(EncounterRules encounterRules, LogicState state) {
      this.encounterRules = encounterRules;
      this.state = state;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddCustomPlayerLanceExtraSpawnPoints] Adding extra player lance spawn points to handle more player units");
      spawnKeys.Clear();
      GameObject playerSpawnGo = GetPlayerSpawn();
      if (playerSpawnGo != null) {
        IncreaseLanceSpawnPoints(playerSpawnGo);
      }

      if (state != null) {
        state.Set("ExtraLanceSpawnKeys", spawnKeys);
      }
    }

    private void IncreaseLanceSpawnPoints(GameObject playerSpawnGo) {
      SpawnableUnit[] lanceUnits = MissionControl.Instance.CurrentContract.Lances.GetLanceUnits(EncounterRules.PLAYER_TEAM_ID);
      Main.Logger.Log($"[AddCustomPlayerLanceExtraSpawnPoints] '{lanceUnits.Length}' player lance units are being sent to Mission Control by Bigger Drops.");
      List<GameObject> unitSpawnPoints = playerSpawnGo.FindAllContains("SpawnPoint");
      if (unitSpawnPoints.Count <= 0) {
        Main.Logger.LogError($"[AddCustomPlayerLanceExtraSpawnPoints] Player spawner '{playerSpawnGo.name}' has no existing spawn points.");
        return;
      }

      GameObject orientationUnit = unitSpawnPoints[0];
      string orientationKey = $"{playerSpawnGo.name}.{orientationUnit.name}";
      if (encounterRules != null) {
        encounterRules.ObjectLookup[orientationKey] = orientationUnit;
      }

      // Keep the original player formation as the anchor set so multiple extra units do not chain further and further away
      List<GameObject> originalSpawnPoints = new List<GameObject>(unitSpawnPoints);

      for (int i = unitSpawnPoints.Count; i < lanceUnits.Length; i++) {
        UnitSpawnPointGameLogic created = CreateSpawn(i + 1, playerSpawnGo, originalSpawnPoints);
        if (created != null && encounterRules != null) {
          string spawnKey = $"{playerSpawnGo.name}.{created.gameObject.name}";
          encounterRules.ObjectLookup[spawnKey] = created.gameObject;
          spawnKeys.Add(new string[] { spawnKey, orientationKey });
        }
      }
    }

    private UnitSpawnPointGameLogic CreateSpawn(int spawnNumber, GameObject playerSpawnGo, List<GameObject> anchorSpawnPoints) {
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      GameObject orientationUnit = anchorSpawnPoints[0];
      Vector3 anchorWorldPos = anchorSpawnPoints.GetRandom().transform.position;
      Vector3 spawnWorldPos = Vector3.zero;
      bool found = false;

      // Note: ChunkLogic runs in InitializeContract Prefix before LoadMapMetaDataV2 populates cachedHeight/cachedSteepness,
      // so place on a distinct in-bounds world hex here and let SpawnObjectsAroundTarget validate terrain/reachability in SCENE_MANIPULATION
      for (int attempt = 0; attempt < 25; attempt++) {
        Vector3 sampleAnchor = anchorSpawnPoints.GetRandom().transform.position;
        Vector3 candidate = SceneUtils.GetRandomPositionFromTarget(sampleAnchor, 24, 100).GetClosestHexLerpedPointOnGrid();
        if (!encounterLayerData.IsInEncounterBounds(candidate)) continue;
        if (candidate.IsTooCloseToAnotherSpawn()) continue;

        spawnWorldPos = candidate;
        found = true;
        break;
      }

      if (!found && combatState != null) {
        foreach (GameObject anchorGo in anchorSpawnPoints) {
          Vector3 pos = anchorGo.transform.position;
          List<Vector3> ringHexes = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(pos, 15);
          ringHexes.Sort((a, b) => (a - pos).sqrMagnitude.CompareTo((b - pos).sqrMagnitude));
          foreach (Vector3 rawHex in ringHexes) {
            Vector3 hex = rawHex.GetClosestHexLerpedPointOnGrid();
            if (!encounterLayerData.IsInEncounterBounds(hex)) continue;
            if (hex.IsTooCloseToAnotherSpawn()) continue;

            spawnWorldPos = hex;
            found = true;
            break;
          }
          if (found) break;
        }
      }

      if (!found) {
        // Keep last-resort initial placement inside encounter bounds rather than offsetting blindly across a boundary
        Vector3 inBoundsFallback = SceneUtils.GetRandomPositionWithinBounds(anchorWorldPos, 240f).GetClosestHexLerpedPointOnGrid();
        spawnWorldPos = encounterLayerData.IsInEncounterBounds(inBoundsFallback)
          ? inBoundsFallback
          : anchorWorldPos.GetClosestHexLerpedPointOnGrid();
      }

      Vector3 localPos = playerSpawnGo.transform.InverseTransformPoint(spawnWorldPos);
      Main.Logger.Log($"[AddCustomPlayerLanceExtraSpawnPoints] Creating lance 'Player Lance' spawn point 'UnitSpawnPoint{spawnNumber}' at world '{spawnWorldPos}'");
      UnitSpawnPointGameLogic created = LanceSpawnerFactory.CreateUnitSpawnPoint(playerSpawnGo, $"UnitSpawnPoint{spawnNumber}", localPos, Guid.NewGuid().ToString());
      created.transform.rotation = orientationUnit.transform.rotation;
      return created;
    }
  }
}