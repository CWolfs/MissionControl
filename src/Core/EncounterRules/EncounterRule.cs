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

    public virtual void Run(LogicBlock.LogicType type) {
      IEnumerable<LogicBlock> logicBlocks = EncounterLogic.Where(logic => logic.Type == type);

      switch(type) {
        case LogicBlock.LogicType.RESOURCE_REQUEST:
          RunResourceRequestLogic(logicBlocks);
          break;
        case LogicBlock.LogicType.ENCOUNTER_MANIPULATION:
          RunEncounterManipulationLogic(logicBlocks);
          break;
        case LogicBlock.LogicType.SCENE_MANIPULATION:
          RunSceneManipulationLogic(logicBlocks);
          break;
        default:
          Main.Logger.LogError($"[EncounterRules] Unknown logic type '{type}'");
          break;
      }
    }

    private void RunResourceRequestLogic(IEnumerable<LogicBlock> logicBlocks) {
       Main.Logger.LogError($"[EncounterRules] RunResourceRequest logic processing not yet implemented");
    }

    private void RunEncounterManipulationLogic(IEnumerable<LogicBlock> logicBlocks) {
      Main.Logger.LogError($"[EncounterRules] RunEncounterManipulation logic processing not yet implemented");
    }

    private void RunSceneManipulationLogic(IEnumerable<LogicBlock> logicBlocks) {
      EncounterLayerGo = SpawnManager.GetInstance().EncounterLayerGameObject;
      ChunkPlayerLanceGo = EncounterLayerGo.transform.Find("Chunk_PlayerLance").gameObject;
      SpawnerPlayerLanceGo = ChunkPlayerLanceGo.transform.Find("Spawner_PlayerLance").gameObject;
      ObjectLookup.Add("ChunkPlayerLance", ChunkPlayerLanceGo);
      ObjectLookup.Add("SpawnerPlayerLance", SpawnerPlayerLanceGo);

      LinkObjectReferences();

      foreach (SpawnLogic spawnLogic in logicBlocks) {
        spawnLogic.Run();
      }
    }
  }
}