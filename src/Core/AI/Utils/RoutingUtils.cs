using UnityEngine;

using BattleTech;

namespace MissionControl.AI {
  public class RoutingUtils {
    public static bool IsUnitInsideRadiusOfPoint(AbstractActor unit, Vector3 target, float radius) {{
      float magnitude = (unit.CurrentPosition - target).magnitude;
      if (magnitude > radius) return false;}
      return true;
    }
  }
}