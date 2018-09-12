using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace SpawnVariation.Logic {
  public class SpawnLanceAtEdgeOfBoundary : SpawnLogic {
    public SpawnLanceAtEdgeOfBoundary(GameObject lanceGameObject) : base() {
      Main.Logger.Log($"[SpawnLanceAtEdgeOfBoundary] For {lanceGameObject.name}");
    }
  }
}