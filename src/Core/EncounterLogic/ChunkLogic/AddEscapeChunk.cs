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
  public class AddEscapeChunk : ChunkLogic {

    public AddEscapeChunk() {
      
    }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddEscapeChunk] Adding encounter structure");
    }
  }
}