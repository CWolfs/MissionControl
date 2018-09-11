using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

namespace SpawnVariation.Core {
  public class SpawnManager {
    private static SpawnManager instance;

    public static SpawnManager GetInstance() { 
      if (instance == null) instance = new SpawnManager();
      return instance;
    }

    private SpawnManager() { }
  }
}