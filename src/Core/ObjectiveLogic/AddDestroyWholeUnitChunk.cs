using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Logic;
using SpawnVariation.Rules;
using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class AddDestroyWholeUnitChunk : LanceLogic {
    public AddDestroyWholeUnitChunk() { }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddDestroyWholeUnitChunk] Adding encounter structure");
    }
  }
}