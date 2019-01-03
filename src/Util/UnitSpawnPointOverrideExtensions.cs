using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using MissionControl;

using BattleTech.Framework;

public static class UnitSpawnPointOverrideExtensions {
  public static UnitSpawnPointOverride DeepCopy(this UnitSpawnPointOverride unitSpawnPointOverride) {  
    UnitSpawnPointOverride unit = unitSpawnPointOverride.Copy();
    unit.unitSpawnPoint = new UnitSpawnPointRef();
    unit.unitSpawnPoint.EncounterObjectGuid = Guid.NewGuid().ToString();
    return unit;
  }
}