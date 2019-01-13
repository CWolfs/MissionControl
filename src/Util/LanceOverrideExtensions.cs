using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using MissionControl;

using BattleTech.Framework;

public static class LanceOverrideExtensions {
  public static UnitSpawnPointOverride GetAnyTaggedLanceMember(this LanceOverride lanceOverride) {  
      List<UnitSpawnPointOverride> unitSpawnOverrides = lanceOverride.unitSpawnPointOverrideList;
      foreach (UnitSpawnPointOverride unitSpawnOverride in unitSpawnOverrides) {
        if ((unitSpawnOverride.unitDefId == "Tagged") || (unitSpawnOverride.unitDefId == "UseLance")) return unitSpawnOverride;
      }
      return null;  
  }
}