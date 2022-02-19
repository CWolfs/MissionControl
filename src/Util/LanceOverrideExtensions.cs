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
      if (unit.selectedUnitType == BattleTech.UnitType.Turret) return true;
    }

    return false;
  }

  public static bool ContainsTurretTag(this LanceOverride lanceOverride) {
    return lanceOverride.lanceTagSet.Contains("lance_type_turret");
  }

  public static List<int> GetUnresolvedUnitIndexes(this LanceOverride lanceOverride, int startingIndex) {
    bool isManualLance = lanceOverride.lanceDefId.ToLower() == "manual";
    Debug.Log($"[ExtendedLances.GetUnresolvedUnitIndexes] Running with Autofill Type '{MissionControl.Main.Settings.ExtendedLances.AutofillType}' and '{(isManualLance ? "Manual" : "Tagged / Direct Reference")} Lance'");
    List<int> unresolvedUnitIndexes = new List<int>();

    if (MissionControl.Main.Settings.ExtendedLances.AutofillType == "RespectEmpty") {
      if (isManualLance) {
        // RespectEmpty and Manual Lance - Start counting from the original UnitOverride count up to the end
        for (int i = startingIndex; i < lanceOverride.unitSpawnPointOverrideList.Count; i++) {
          UnitSpawnPointOverride unitOverride = lanceOverride.unitSpawnPointOverrideList[i];
          if (unitOverride.IsUnresolved()) unresolvedUnitIndexes.Add(i);
        }
      } else { // Tagged/Direct Lance Reference
        // RespectEmpty and Tagged/Direct Referenced Lance - Start counting from the LanceDef size up to the end
        LanceDef loadedLanceDef = (LanceDef)AccessTools.Field(typeof(LanceOverride), "loadedLanceDef").GetValue(lanceOverride);
        if (loadedLanceDef == null) return unresolvedUnitIndexes;

        for (int i = loadedLanceDef.LanceUnits.Length; i < lanceOverride.unitSpawnPointOverrideList.Count; i++) {
          UnitSpawnPointOverride unitOverride = lanceOverride.unitSpawnPointOverrideList[i];
          if (unitOverride.IsUnresolved()) unresolvedUnitIndexes.Add(i);
        }
      }
    } else { // FillEmpty
      // FillEmpty and Manual/Tagged/Direct Reference Lance- Start looking at all UnitOverrides and fill empty up to the end
      for (int i = 0; i < lanceOverride.unitSpawnPointOverrideList.Count; i++) {
        UnitSpawnPointOverride unitOverride = lanceOverride.unitSpawnPointOverrideList[i];
        if (unitOverride.IsUnresolved()) unresolvedUnitIndexes.Add(i);
      }
    }

    return unresolvedUnitIndexes;
  }

  public static UnitSpawnPointOverride GetUnitToCopy(this LanceOverride lanceOverride) {
    if (lanceOverride.unitSpawnPointOverrideList.Count <= 0) {
      MissionControl.Main.LogDebug("[GetUnitToCopy] No UnitSpawnPointOverrides in lance. No unit to copy");
      return null;
    }

    UnitSpawnPointOverride originalUnitSpawnPointOverride = lanceOverride.GetAnyTaggedLanceMember();
    // If there are only manual units - then select one at random from the Lance. Previously this selected copies of the first unit in the lance
    if (originalUnitSpawnPointOverride == null) {
      MissionControl.Main.LogDebug($"[LanceOverrideExtensions.GetUnitToCopy] Using '{MissionControl.Main.Settings.ExtendedLances.AutofillUnitCopyType}' to get unit to copy");
      if (MissionControl.Main.Settings.ExtendedLances.AutofillUnitCopyType == "FirstInLance") {
        originalUnitSpawnPointOverride = lanceOverride.unitSpawnPointOverrideList[0];
      } else { // RandomInLance
        originalUnitSpawnPointOverride = lanceOverride.GetRandomNonEmptyUnit();
      }
    }
    return originalUnitSpawnPointOverride;
  }

  public static UnitSpawnPointOverride GetRandomNonEmptyUnit(this LanceOverride lanceOverride) {
    if (lanceOverride.unitSpawnPointOverrideList.Count <= 0) {
      MissionControl.Main.LogDebug("[GetRandomNonEmptyUnit] No UnitSpawnPointOverrides in lance. No unit to copy");
      return null;
    }

    for (int i = 0; i < 10; i++) {
      UnitSpawnPointOverride unitOverride = lanceOverride.unitSpawnPointOverrideList.GetRandom();
      if (!unitOverride.IsUnitDefNone) {
        return unitOverride;
      }
    }
    return lanceOverride.unitSpawnPointOverrideList[0]; // Fallback
  }
}