using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Logic;
using MissionControl.Rules;
using MissionControl.EncounterFactories;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class AddDestroyWholeUnitChunk : ObjectiveLogic {
    private string teamGuid;
    private string lanceGuid;
    private List<string> unitGuids;
    private string spawnerName;
    private string objectiveLabel;

    public AddDestroyWholeUnitChunk(string teamGuid, string lanceGuid, List<string> unitGuids, string spawnerName, string objectiveLabel) {
      this.teamGuid = teamGuid;
      this.lanceGuid = lanceGuid;
      this.unitGuids = unitGuids;
      this.spawnerName = spawnerName;
      this.objectiveLabel = objectiveLabel;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddDestroyWholeUnitChunk] Adding encounter structure");
      EncounterLayerData encounterLayerData = MissionControl.GetInstance().EncounterLayerData;
      DestroyWholeLanceChunk destroyWholeChunk = ChunkFactory.CreateDestroyWholeLanceChunk();
      destroyWholeChunk.encounterObjectGuid = System.Guid.NewGuid().ToString();

      bool spawnOnActivation = true;
      LanceSpawnerGameLogic lanceSpawner = LanceSpawnerFactory.CreateLanceSpawner(
        destroyWholeChunk.gameObject,
        spawnerName,
        lanceGuid,
        teamGuid,
        spawnOnActivation,
        SpawnUnitMethodType.InstantlyAtSpawnPoint,
        unitGuids
      );
      LanceSpawnerRef lanceSpawnerRef = new LanceSpawnerRef(lanceSpawner);

      bool showProgress = true;
      int priority = -10;
      bool displayToUser = true;
      DestroyLanceObjective objective = ObjectiveFactory.CreateDestroyLanceObjective(
        destroyWholeChunk.gameObject,
        lanceSpawnerRef,
        lanceGuid,
        objectiveLabel,
        showProgress,
        ProgressFormat.PERCENTAGE_COMPLETE,
        "The primary objective to destroy the enemy lance",
        priority,
        displayToUser,
        ObjectiveMark.AttackTarget
      );

      DestroyLanceObjectiveRef destroyLanceObjectiveRef = new DestroyLanceObjectiveRef();
      destroyLanceObjectiveRef.encounterObject = objective;

      destroyWholeChunk.lanceSpawner = lanceSpawnerRef;
      destroyWholeChunk.destroyObjective = destroyLanceObjectiveRef;
    }
  }
}