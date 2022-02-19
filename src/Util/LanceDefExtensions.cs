using System.Collections.Generic;

using BattleTech;

public static class LanceDefExtensions {
  public static bool IsATurretLance(this LanceDef lanceDef) {
    return lanceDef.ContainsTurretTag() || lanceDef.ContainsAtLeastOneTurret();
  }

  public static bool ContainsAtLeastOneTurret(this LanceDef lanceDef) {
    List<LanceDef.Unit> units = new List<LanceDef.Unit>(lanceDef.LanceUnits);

    for (int i = 0; i < units.Count; i++) {
      LanceDef.Unit unit = units[i];
      if (unit.unitType == BattleTech.UnitType.Turret) return true;
    }

    return false;
  }

  public static bool ContainsTurretTag(this LanceDef lanceDef) {
    return lanceDef.LanceTags.Contains("lance_type_turret");
  }
}