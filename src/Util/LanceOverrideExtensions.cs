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
      if ((unitSpawnOverride.unitDefId == "Tagged") || (unitSpawnOverride.unitDefId == "UseLance") || (unitSpawnOverride.unitDefId == "mechDef_InheritLance") || (unitSpawnOverride.unitDefId == "vehicleDef_InheritLance") || (unitSpawnOverride.unitDefId == "turretDef_InheritLance")) return unitSpawnOverride;
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
    Debug.Log($"[ExtendedLances.GetUnresolvedUnitIndexes] Running with Autofill Type ${MissionControl.Main.Settings.ExtendedLances.AutofillType}");
    List<int> unresolvedUnitIndexes = new List<int>();

    if (MissionControl.Main.Settings.ExtendedLances.AutofillType == "RespectEmpty") {
      if (lanceOverride.unitSpawnPointOverrideList.Count > 4) {
        for (int i = 4; i < lanceOverride.unitSpawnPointOverrideList.Count; i++) {
          UnitSpawnPointOverride unitOverride = lanceOverride.unitSpawnPointOverrideList[i];
          if (unitOverride.IsUnresolved()) unresolvedUnitIndexes.Add(i);
        }
      }
    } else {
      LanceDef loadedLanceDef = (LanceDef)AccessTools.Field(typeof(LanceOverride), "loadedLanceDef").GetValue(lanceOverride);
      if (loadedLanceDef == null) return unresolvedUnitIndexes;

      if (lanceOverride.unitSpawnPointOverrideList.Count > loadedLanceDef.LanceUnits.Length) {
        for (int i = loadedLanceDef.LanceUnits.Length; i < lanceOverride.unitSpawnPointOverrideList.Count; i++) {
          UnitSpawnPointOverride unitOverride = lanceOverride.unitSpawnPointOverrideList[i];
          if (unitOverride.IsUnresolved()) unresolvedUnitIndexes.Add(i);
        }
      }
    }

    return unresolvedUnitIndexes;
  }
}