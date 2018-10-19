using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using BattleTech;

using MissionControl;

public static class HexGridUtils {
  public static List<Vector3> GetGridPointsAroundPointWithinRadius(this HexGrid hexGrid, Vector3 position, int minRadius, int maxRadius) {  
    List<Vector3> selectedPositions = new List<Vector3>();
    CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
    List<Vector3> toMaxRadius = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(position, maxRadius);
    for (int i = 0; i < toMaxRadius.Count; i++) {
      Vector3 pos = toMaxRadius[i];
      if (position.DistanceFlat(pos) > minRadius) selectedPositions.Add(pos);
    }
    return selectedPositions;
  }
}