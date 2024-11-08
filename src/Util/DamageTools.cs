

using BattleTech;

namespace MissionControl.Utils {
  public class DamageTools {
    public static LocationDamageLevel CalculateDamageLevel(float currentStructure, float maxStructure) {
      float num = 1f - currentStructure / maxStructure;
      return ((num >= 1f) ? LocationDamageLevel.Destroyed : ((num >= 0.71f) ? LocationDamageLevel.NonFunctional : ((num >= 0.41f) ? LocationDamageLevel.Penalized : LocationDamageLevel.Functional)));
    }
  }
}