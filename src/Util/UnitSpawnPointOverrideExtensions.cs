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

  public static bool IsUnresolved(this UnitSpawnPointOverride unitSpawnPointOverride) {
    if ((unitSpawnPointOverride.unitDefId == "Tagged") || (unitSpawnPointOverride.unitDefId == "UseLance") ||
    (unitSpawnPointOverride.unitDefId == "mechDef_InheritLance") || (unitSpawnPointOverride.unitDefId == "vehicleDef_InheritLance") ||
    (unitSpawnPointOverride.unitDefId == "mechDef_None") || (unitSpawnPointOverride.unitDefId == "vehicleDef_None") ||
    (unitSpawnPointOverride.unitDefId == "null") || (unitSpawnPointOverride.unitDefId == "")) return true;
    return false;
  }
}