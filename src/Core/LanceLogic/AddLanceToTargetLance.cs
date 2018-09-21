using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using SpawnVariation.Rules;
using SpawnVariation.Utils;

namespace SpawnVariation.Logic {
  public class AddLanceToTargetLance : LanceLogic {
    public AddLanceToTargetLance() { }

    public override void Run(RunPayload payload) {
      Main.Logger.Log($"[AddLanceToTargetLance] Adding lance to target lance");
    }
  }
}