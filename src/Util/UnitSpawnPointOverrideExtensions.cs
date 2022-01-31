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
    // Main.LogDebug($"[UnitSpawnPointOverrideExtensions.IsUnresolved] unitSpawnPointOverride unitDefId is: {unitSpawnPointOverride.unitDefId} and selectedUnitId {unitSpawnPointOverride.selectedUnitDefId}");
    if (unitSpawnPointOverride.selectedUnitDefId == "mechDef_None" || unitSpawnPointOverride.selectedUnitDefId == "vehicleDef_None") return true;
    return false;
  }
}