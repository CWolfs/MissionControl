using UnityEngine;

using BattleTech;

public static class Vector3Extensions {
  public static Vector3 Flattened(this Vector3 vector) {
    return new Vector3(vector.x, 0f, vector.z);
  }

  public static float DistanceFlat(this Vector3 origin, Vector3 destination) {
    return Vector3.Distance(origin.Flattened(), destination.Flattened());
  }

  public static Vector3 GetClosestHexLerpedPointOnGrid(this Vector3 origin) {
    CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
    Vector3 originOnGrid = combatState.HexGrid.GetClosestPointOnGrid(origin);
    originOnGrid.y = combatState.MapMetaData.GetLerpedHeightAt(originOnGrid);
    return originOnGrid;
  }

  // TODO: Find a better way to do this that caches things in a good way. Not so easy as new spawns can be created at any point
  public static bool IsTooCloseToAnotherSpawn(this Vector3 position, GameObject ignoreGo = null) {
    UnitSpawnPointGameLogic[] unitSpawns = MissionControl.MissionControl.Instance.EncounterLayerData.GetComponentsInChildren<UnitSpawnPointGameLogic>();
    // MissionControl.Main.LogDebug($"[IsTooCloseToAnotherSpawn] There are {unitSpawns.Length} unit spawns found.");

    foreach (UnitSpawnPointGameLogic unitSpawn in unitSpawns) {
      // Guard: Ignore the object that the position originated from
      if ((ignoreGo != null) && (unitSpawn.name == ignoreGo.name)) {
        // MissionControl.Main.LogDebug($"[IsTooCloseToAnotherSpawn] Checked target position '{position}' and found object the position originated from. Safely ignoring this for '{unitSpawn.name}'");
        continue;
      }

      float distance = Vector3.Distance(unitSpawn.transform.position, position);
      if (distance < 13) {
        MissionControl.Main.LogDebug($"[IsTooCloseToAnotherSpawn] Too close to another spawn. Focus target is '{position}' and checked against '{unitSpawn.transform.position}'");
        return true;
      }
    }

    return false;
  }
}