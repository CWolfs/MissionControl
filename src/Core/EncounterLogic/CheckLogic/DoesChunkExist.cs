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
  public class DoesChunkExist : ChunkLogic {
    private LogicState state = null;
    private string chunkName = "";

    public DoesChunkExist(LogicState state, string chunkName) {
      this.state = state;
      this.chunkName = chunkName;
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[DoesChunkExist] Checking if '{this.chunkName}' exists");
      GameObject encounterLayerGameObject = MissionControl.Instance.EncounterLayerGameObject;
      GameObject result = encounterLayerGameObject.FindRecursive(this.chunkName);
      if (result != null) state.Set($"{chunkName}_Exists", true);
    }
  }
}