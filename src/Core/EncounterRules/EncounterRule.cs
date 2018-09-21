using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using SpawnVariation.Logic;

namespace SpawnVariation.Rules {
  public abstract class EncounterRule {
    protected GameObject EncounterLayerGo { get; set; }
    protected GameObject ChunkPlayerLanceGo { get; set; }
    protected GameObject SpawnerPlayerLanceGo { get; set; }

    protected List<LogicBlock> EncounterLogic = new List<LogicBlock>();
    public Dictionary<string, GameObject> ObjectLookup = new Dictionary<string, GameObject>();

    public EncounterRule() { }

    public abstract void LinkObjectReferences();

    public virtual void Run(LogicBlock.LogicType type, RunPayload payload) {
      IEnumerable<LogicBlock> logicBlocks = EncounterLogic.Where(logic => logic.Type == type);

      switch(type) {
        case LogicBlock.LogicType.RESOURCE_REQUEST:
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
      EncounterLayerGo = SpawnManager.GetInstance().EncounterLayerGameObject;
      ChunkPlayerLanceGo = EncounterLayerGo.transform.Find("Chunk_PlayerLance").gameObject;
      SpawnerPlayerLanceGo = ChunkPlayerLanceGo.transform.Find("Spawner_PlayerLance").gameObject;
      ObjectLookup.Add("ChunkPlayerLance", ChunkPlayerLanceGo);
      ObjectLookup.Add("SpawnerPlayerLance", SpawnerPlayerLanceGo);

      LinkObjectReferences();

      foreach (SpawnLogic spawnLogic in logicBlocks) {
        spawnLogic.Run(payload);
      }
    }

    public List<string> GenerateGuids(int amountRequired) {
      List<string> guids = new List<string>();

      for (int i = 0; i < amountRequired; i++) {
        guids.Add(Guid.NewGuid().ToString());
      }

      return guids;
    }
  }
}