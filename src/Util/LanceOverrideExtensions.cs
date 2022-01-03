using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using MissionControl;

using Harmony;

using BattleTech;
using BattleTech.Framework;

public static class LanceOverrideExtensions {
  public static UnitSpawnPointOverride GetAnyTaggedLanceMember(this LanceOverride lanceOverride) {
    List<UnitSpawnPointOverride> unitSpawnOverrides = lanceOverride.unitSpawnPointOverrideList;
    foreach (UnitSpawnPointOverride unitSpawnOverride in unitSpawnOverrides) {
      if ((unitSpawnOverride.unitDefId == "Tagged") || (unitSpawnOverride.unitDefId == "UseLance") || (unitSpawnOverride.unitDefId == "mechDef_InheritLance") || (unitSpawnOverride.unitDefId == "vehicleDef_InheritLance")) return unitSpawnOverride;
    }
    return null;
  }

  public static bool IsATurretLance(this LanceOverride lanceOverride) {
    return lanceOverride.ContainsTurretTag() || lanceOverride.ContainsAtLeastOneTurret();
  }

  public static bool ContainsAtLeastOneTurret(this LanceOverride lanceOverride) {
    List<UnitSpawnPointOverride> units = lanceOverride.unitSpawnPointOverrideList;

    for (int i = 0; i < units.Count; i++) {
      UnitSpawnPointOverride unit = units[i];
      if (unit.unitType == BattleTech.UnitType.Turret) return true;
    }

    return false;
  }

  public static bool ContainsTurretTag(this LanceOverride lanceOverride) {
    return lanceOverride.lanceTagSet.Contains("lance_type_turret");
  }

  public static List<int> GetUnresolvedUnitIndexes(this LanceOverride lanceOverride) {
    List<int> unresolvedUnitIndexes = new List<int>();

    // if (MissionControl.Main.Settings.ExtendedLances.) 
    // TODO: Expose to a setting to give the option of the old behaviour or the new 'from the lancedef size onward'
    LanceDef loadedLanceDef = (LanceDef)AccessTools.Field(typeof(LanceOverride), "loadedLanceDef").GetValue(lanceOverride);

    if (lanceOverride.unitSpawnPointOverrideList.Count > loadedLanceDef.LanceUnits.Length) {
      for (int i = loadedLanceDef.LanceUnits.Length; i < lanceOverride.unitSpawnPointOverrideList.Count; i++) {
        UnitSpawnPointOverride unitOverride = lanceOverride.unitSpawnPointOverrideList[i];
        if (unitOverride.IsUnresolved()) unresolvedUnitIndexes.Add(i);
      }
    }

    return unresolvedUnitIndexes;
  }
}