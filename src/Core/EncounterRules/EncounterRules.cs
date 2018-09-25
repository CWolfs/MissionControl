using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Logic;

namespace MissionControl.Rules {
  public abstract class EncounterRules {
    public enum EncounterState { NOT_STARTED, RUNNING, FAILED, FINISHED };

    public const string PLAYER_TEAM_ID = "bf40fd39-ccf9-47c4-94a6-061809681140";
    public const string EMPLOYER_TEAM_ID = "ecc8d4f2-74b4-465d-adf6-84445e5dfc230";
    public const string TARGET_TEAM_ID = "be77cadd-e245-4240-a93e-b99cc98902a5";
    public const string TARGETS_ALLY_TEAM_ID = "31151ed6-cfc2-467e-98c4-9ae5bea784cf";

    protected GameObject EncounterLayerGo { get; set; }
    protected EncounterLayerData EncounterLayerData { get; private set; }
    protected GameObject ChunkPlayerLanceGo { get; set; }
    protected GameObject SpawnerPlayerLanceGo { get; set; }

    protected List<LogicBlock> EncounterLogic = new List<LogicBlock>();
    public Dictionary<string, GameObject> ObjectLookup = new Dictionary<string, GameObject>();

    public EncounterState State { get; protected set; } = EncounterState.NOT_STARTED;

    public EncounterRules() { }

    public abstract void Build();

    public abstract void LinkObjectReferences(string mapName);

    public virtual void Run(LogicBlock.LogicType type, RunPayload payload) {
      IEnumerable<LogicBlock> logicBlocks = EncounterLogic.Where(logic => logic.Type == type);

      switch(type) {
        case LogicBlock.LogicType.RESOURCE_REQUEST:
          State = EncounterState.RUNNING;
          RunGeneralLogic(logicBlocks, payload);
          break;
        case LogicBlock.LogicType.CONTRACT_OVERRIDE_MANIPULATION:
          RunGeneralLogic(logicBlocks, payload);
          break;
        case LogicBlock.LogicType.ENCOUNTER_MANIPULATION:
          RunGeneralLogic(logicBlocks, payload);
          break;
        case LogicBlock.LogicType.SCENE_MANIPULATION:
          RunSceneManipulationLogic(logicBlocks, payload);
          break;
        default:
          Main.Logger.LogError($"[EncounterRules] Unknown logic type '{type}'");
          break;
      }
    }

    private void RunGeneralLogic(IEnumerable<LogicBlock> logicBlocks, RunPayload payload) {
      foreach (LogicBlock logicBlock in logicBlocks) {
        logicBlock.Run(payload);
      }
    }

    private void RunSceneManipulationLogic(IEnumerable<LogicBlock> logicBlocks, RunPayload payload) {
      EncounterLayerGo = MissionControl.GetInstance().EncounterLayerGameObject;
      EncounterLayerData = MissionControl.GetInstance().EncounterLayerData;
      ChunkPlayerLanceGo = EncounterLayerGo.transform.Find("Chunk_PlayerLance").gameObject;
      SpawnerPlayerLanceGo = ChunkPlayerLanceGo.transform.Find("Spawner_PlayerLance").gameObject;
      ObjectLookup.Add("ChunkPlayerLance", ChunkPlayerLanceGo);
      ObjectLookup.Add("SpawnerPlayerLance", SpawnerPlayerLanceGo);

      string mapName = MissionControl.GetInstance().ContractMapName;

      LinkObjectReferences(mapName);

      if (State == EncounterState.RUNNING) {
        try {
          foreach (SpawnLogic spawnLogic in logicBlocks) {
            spawnLogic.Run(payload);
          }
        } catch (Exception e) {
          Main.Logger.LogError($"[{this.GetType().Name}] Encounter has failed. A LogicBlock failed. Full stack trace - {e}");
        }
      } else {
        Main.Logger.LogError($"[{this.GetType().Name}] Encounter has failed");
        return;
      }

      State = EncounterState.FINISHED;
    }

    public List<string> GenerateGuids(int amountRequired) {
      List<string> guids = new List<string>();

      for (int i = 0; i < amountRequired; i++) {
        guids.Add(Guid.NewGuid().ToString());
      }

      return guids;
    }

    protected GameObject GetClosestPlotName(Vector3 origin) {
      GameObject plotsParentGo = GameObject.Find("PlotParent");
      GameObject closestPlot = null;
      float closestDistance = -1;

      foreach (Transform t in plotsParentGo.transform) {
        Vector3 plotPosition = t.position;
        if (EncounterLayerData.IsInEncounterBounds(plotPosition)) {
          float distance = Vector3.Distance(t.position, origin);
          if (closestDistance == -1 || closestDistance < distance) {
            closestDistance = distance;
            closestPlot = t.gameObject;
          }
        }
      }

      return closestPlot;
    }

    protected string GetPlotBaseName(string mapName) {
      Vector3 playerLanceSpawnPosition = SpawnerPlayerLanceGo.transform.position;
      GameObject plot = GetClosestPlotName(playerLanceSpawnPosition);
      
      if (plot == null) {
        Main.Logger.LogError($"[{this.GetType().Name}] GetPlotBaseName for map {mapName} is empty");
        State = EncounterState.FAILED;
        return "";
      }

      return plot.name;
    }
  }
}