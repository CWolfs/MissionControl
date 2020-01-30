using UnityEngine;
using System;
using System.Collections.Generic;

using BattleTech;

using Harmony;

using HBS.Collections;

using MissionControl.Utils;

namespace MissionControl {
  public class PathFinderManager {
    private static PathFinderManager instance;
    public static PathFinderManager Instance {
      get {
        if (instance == null) instance = new PathFinderManager();
        return instance;
      }
    }

    private static float MAX_SLOPE_FOR_PATHFINDING = 39f;

    private Mech pathFinderMech;
    private Vehicle pathFinderVehicle;

    private Dictionary<string, float> estimatesOfBadPathfings = new Dictionary<string, float>();

    private PathFinderManager() {
      Init();
    }

    public void Init() {
      pathFinderMech = CreatePathFinderMech();
      pathFinderVehicle = CreatePathFindingVehicle();
    }

    private Mech CreatePathFinderMech() {
      GameInstance game = UnityGameInstance.BattleTechGame;
      CombatGameState combatState = game.Combat;
      string spawnerId = Guid.NewGuid().ToString();
      string uniqueId = $"{spawnerId}.9999999999";

      HeraldryDef heraldryDef = null;
      combatState.DataManager.Heraldries.TryGet(HeraldryDef.HeraldyrDef_SinglePlayerSkirmishPlayer1, out heraldryDef);

      MechDef mechDef = null;
      combatState.DataManager.MechDefs.TryGet("mechdef_spider_SDR-5V", out mechDef);

      PilotDef pilotDef = null;
      combatState.DataManager.PilotDefs.TryGet("pilot_default", out pilotDef);
      Mech mech = new Mech(mechDef, pilotDef, new TagSet(), uniqueId, combatState, spawnerId, heraldryDef);
      return mech;
    }

    private Vehicle CreatePathFindingVehicle() {
      GameInstance game = UnityGameInstance.BattleTechGame;
      CombatGameState combatState = game.Combat;
      string spawnerId = Guid.NewGuid().ToString();
      string uniqueId = $"{spawnerId}.9999999998";

      HeraldryDef heraldryDef = null;
      combatState.DataManager.Heraldries.TryGet(HeraldryDef.HeraldyrDef_SinglePlayerSkirmishPlayer1, out heraldryDef);

      VehicleDef vehicleDef = null;
      combatState.DataManager.VehicleDefs.TryGet("vehicledef_DEMOLISHER", out vehicleDef);

      PilotDef pilotDef = null;
      combatState.DataManager.PilotDefs.TryGet("pilot_default", out pilotDef);
      Vehicle vehicle = new Vehicle(vehicleDef, pilotDef, new TagSet(), uniqueId, combatState, spawnerId, heraldryDef);
      return vehicle;
    }

    public void RequestPathFinderMech() {
      Main.LogDebug("[PFM] Requesting path finder mech");
      if (pathFinderMech == null) Init();
      DataManager.Instance.RequestResourcesAndProcess(BattleTechResourceType.Prefab, pathFinderMech.MechDef.Chassis.PrefabIdentifier);
    }

    public void RequestPathFinderVehicle() {
      Main.LogDebug("[PFM] Requesting path finder vehicle");
      if (pathFinderVehicle == null) Init();
      DataManager.Instance.RequestResourcesAndProcess(BattleTechResourceType.Prefab, pathFinderVehicle.VehicleDef.Chassis.PrefabIdentifier);
    }

    private AbstractActor GetPathFindingActor(UnitType type) {
      if (type == UnitType.Mech) {
        return pathFinderMech;
      } else if (type == UnitType.Vehicle) {
        return pathFinderVehicle;
      }

      return null;
    }

    private void SetupPathfindingActor(Vector3 position, AbstractActor pathfindingActor) {
      if (pathfindingActor.GameRep == null) {
        pathfindingActor.Init(position, 0, true);
        pathfindingActor.InitGameRep(null);
      } else {
        pathfindingActor.CurrentPosition = position;
        pathfindingActor.GameRep.transform.position = position;
        pathfindingActor.ResetPathing(false);
      }
    }

    public bool IsSpawnValid(GameObject spawnGo, Vector3 position, Vector3 validityPosition, UnitType type, string identifier) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      MapTerrainDataCell cellData = combatState.MapMetaData.GetCellAt(position);

      Main.LogDebug($"");
      Main.LogDebug($"-------- [PFM.IsSpawnValid] [{identifier}] --------");

      if (position.IsTooCloseToAnotherSpawn(spawnGo)) {
        Main.LogDebug($"[PFM] Position '{position}' is too close to another spawn point. Not a valid location.");
        return false;
      }

      if (cellData.cachedSteepness > MAX_SLOPE_FOR_PATHFINDING) {
        Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Spawn point of '{cellData.cachedSteepness}' is too steep (> {MAX_SLOPE_FOR_PATHFINDING}). Not a valid spawn");
        return false;
      }

      if (IsCellImpassableOrDeepWater(cellData)) return false;
      if (!encounterLayerData.IsInEncounterBounds(position)) return false;
      if (cellData.cachedHeight > (cellData.terrainHeight + 50f)) return false;

      float pathFindingZoneRadius = 25f;
      AbstractActor pathfindingActor = GetPathFindingActor(type);
      SetupPathfindingActor(position, pathfindingActor);

      PathNode positionPathNode = null;
      try {
        PathNodeGrid pathfinderPathGrid = pathfindingActor.Pathing.CurrentGrid;
        positionPathNode = pathfinderPathGrid.GetValidPathNodeAt(position, pathfindingActor.Pathing.MaxCost);
        if (positionPathNode == null) {
          Reset();
          return false;
        }
      } catch (Exception e) {
        Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Caught error in 'pathfinderPathGrid.GetValidPathNodeAt' chunk. Flagging as invalid spawn. Select a new spawn point. {e.Message}, {e.StackTrace}");
        WasABadPathfindTest(validityPosition);
        return false;
      }

      if (positionPathNode == null) {
        Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] PositionPathNode not set from 'pathfinderPathGrid.GetValidPathNodeAt'. No valid path found so not a valid spawn.");
        WasABadPathfindTest(validityPosition);
        return false;
      }

      List<Vector3> path = null;
      try {
        DynamicLongRangePathfinder.PointWithCost pointWithCost = new DynamicLongRangePathfinder.PointWithCost(combatState.HexGrid.GetClosestHexPoint3OnGrid(positionPathNode.Position), 0, (validityPosition - positionPathNode.Position).magnitude);
        path = DynamicLongRangePathfinder.GetDynamicPathToDestination(new List<DynamicLongRangePathfinder.PointWithCost>() { pointWithCost }, validityPosition, float.MaxValue, pathfindingActor, false, new List<AbstractActor>(), pathfindingActor.Pathing.CurrentGrid, pathFindingZoneRadius);
      } catch (Exception e) {
        // TODO: Sometimes this gets triggered in very large amounts. It's usually because the SpawnLogic.GetClosestValidPathFindingHex is increasing
        // the radius larger and larger and the checks keep going off the map
        // I need a way to hard abort out of this and either use the original origin of the focus or trigger the rule logic again (random, around a position etc)
        Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Caught error in 'DynamicLongRangePathfinder' chunk. Flagging as invalid spawn. Select a new spawn point. {e.Message}, {e.StackTrace}");
        WasABadPathfindTest(validityPosition);
        return false;
      }

      if (path == null) {
        Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Path not set from DynamicLongRangePathfinder so not a valid spawn.");
        WasABadPathfindTest(validityPosition);
        return false;
      }

      // List<Vector3> path = DynamicLongRangePathfinder.GetPathToDestination(position, float.MaxValue, pathfindingActor, true, pathFindingZoneRadius);
      // List<Vector3> path = DynamicLongRangePathfinder.GetDynamicPathToDestination(position, float.MaxValue, pathfindingActor, true, new List<AbstractActor>(), pathfindingActor.Pathing.CurrentGrid, pathFindingZoneRadius);

      Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Path count is: '{path.Count}', Current position is: '{position}'");

      // GUARD: Against deep water and other impassables that have slipped through
      if (HasPathImpassableOrDeepWaterTiles(combatState, path)) return false;

      if (path != null && path.Count > 1 && (path[path.Count - 1].DistanceFlat(validityPosition) <= pathFindingZoneRadius)) {
        Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Path count is: '{path.Count}', Current position is: '{position}'");
        Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Last point is '{path[path.Count - 1]}', Validity position is '{validityPosition}'");
        Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Distance from last path to valdity position is: '{(path[path.Count - 1].DistanceFlat(validityPosition))}' and is it within zone radius? '{(path[path.Count - 1].DistanceFlat(validityPosition) <= pathFindingZoneRadius)}'");
        Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Has valid long range path finding");
        if (HasValidNeighbours(positionPathNode, validityPosition, type)) {
          Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Has at least two valid neighbours");

          if (HasValidLocalPathfinding(positionPathNode, validityPosition, type)) {
            Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Has a valid path");
            Reset();
            Main.LogDebug($"-------- END [PFM.IsSpawnValid] [{identifier}] END --------");
            Main.LogDebug($"");
            return true;
          } else {
            Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Does NOT have a valid path");
          }
        } else {
          Main.LogDebug($"[PFM.IsSpawnValid] [{identifier}] Does not have two valid neighbours");
        }
      }

      /* // Failed attempt to improve spawn checks
      List<Vector3> path = DynamicLongRangePathfinder.GetDynamicPathToDestination(validityPosition, float.MaxValue, pathfindingActor, true, new List<AbstractActor>(), pathfindingActor.Pathing.CurrentGrid, pathFindingZoneRadius);
      if (path != null && (path[path.Count - 1].DistanceFlat(validityPosition) <= pathFindingZoneRadius)) {
        if (path.Count > 4) { // very strong pathfinding location
          return true;
        } else {
          Main.Logger.Log($"[PFM] Spawn point is valid due to proximity but is not strong enough success for pathing. Attempting to confirm.");
          CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
          List<Vector3> pointsAroundPosition = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(position, 3, 5);

          foreach (Vector3 point in pointsAroundPosition) {
            List<Vector3> secondaryPath = DynamicLongRangePathfinder.GetDynamicPathToDestination(point, float.MaxValue, pathfindingActor, true, new List<AbstractActor>(), pathfindingActor.Pathing.CurrentGrid, 2); 
            if (path != null && path.Count > 2) {
              Main.Logger.Log($"[PFM] Spawn point is valid. It is close to the validation point but can be moved away from. Success.");
              return true;
            }
          }
        }
      }
      */


      Main.LogDebug($"-------- END [PFM.IsSpawnValid] [{identifier}] END --------");
      Main.LogDebug($"");
      Reset();
      return false;
    }

    private bool HasPathImpassableOrDeepWaterTiles(CombatGameState combatState, List<Vector3> path) {
      if (path == null) return true;

      for (int i = 0; i < path.Count; i++) {
        Vector3 position = path[i];
        MapTerrainDataCell cellData = combatState.MapMetaData.GetCellAt(position);
        if (IsCellImpassableOrDeepWater(cellData)) {
          Main.LogDebug("[PFM.HasPathImpassableOrDeepWaterTiles] Path has impassable or deep water tiles in it");
          return true;
        }
      }

      return false;
    }

    private bool IsCellImpassableOrDeepWater(MapTerrainDataCell cellData) {
      TerrainMaskFlags terrainMask = cellData.terrainMask;
      bool isImpassableOrDeepWater = SplatMapInfo.IsImpassable(terrainMask) || (SplatMapInfo.IsDeepWater(terrainMask) && !cellData.MapEncounterLayerDataCell.HasBuilding);
      if (isImpassableOrDeepWater) {
        Main.LogDebug("[PFM.IsCellImpassableOrDeepWater] Tile is impassable or deep water");
        return true;
      }
      return false;
    }

    public bool HasValidNeighbours(PathNode positionNode, Vector3 validityPosition, UnitType type) {
      int count = 0;
      AbstractActor pathfindingActor = GetPathFindingActor(type);
      SetupPathfindingActor(positionNode.Position, pathfindingActor);

      AccessTools.Field(typeof(PathNodeGrid), "open").SetValue(pathfindingActor.Pathing.CurrentGrid, new List<PathNode>() { positionNode });
      pathfindingActor.Pathing.CurrentGrid.UpdateBuild(1);
      List<PathNode> neighbours = AccessTools.Field(typeof(PathNodeGrid), "open").GetValue(pathfindingActor.Pathing.CurrentGrid) as List<PathNode>;

      foreach (PathNode neighbourNode in neighbours) {
        float cost = pathfindingActor.Pathing.CurrentGrid.GetTerrainModifiedCost(positionNode, neighbourNode, pathfindingActor.MaxWalkDistance);
        Main.LogDebug($"[PFM.HasValidNeighbours] Cost of neighbour is {cost} with max pathfinder cost being {pathfindingActor.MaxWalkDistance}");
        if (cost < pathfindingActor.MaxWalkDistance) count++;
        if (count >= 2) return true;
      }
      return false;
    }

    public bool HasValidLocalPathfinding(PathNode positionNode, Vector3 validityPosition, UnitType type) {
      AbstractActor pathfindingActor = GetPathFindingActor(type);
      SetupPathfindingActor(positionNode.Position, pathfindingActor);

      AccessTools.Field(typeof(PathNodeGrid), "open").SetValue(pathfindingActor.Pathing.CurrentGrid, new List<PathNode>() { positionNode });
      pathfindingActor.Pathing.UpdateBuild(10000);
      pathfindingActor.Pathing.UpdateFreePath(validityPosition, validityPosition, false, false);

      Vector3 vectorToTarget = pathfindingActor.Pathing.ResultDestination - pathfindingActor.CurrentPosition;
      float distanceToTarget = vectorToTarget.magnitude;
      if (distanceToTarget > pathfindingActor.MaxWalkDistance) {
        vectorToTarget = vectorToTarget.normalized * pathfindingActor.MaxWalkDistance;
      }

      pathfindingActor.Pathing.UpdateFreePath(validityPosition, validityPosition, false, false);
      if (pathfindingActor.Pathing.HasPath) return true;

      return false;
    }

    public void WasABadPathfindTest(Vector3 pathfindTarget) {
      if (!estimatesOfBadPathfings.ContainsKey(pathfindTarget.ToString())) {
        estimatesOfBadPathfings.Add(pathfindTarget.ToString(), 0);
      }

      estimatesOfBadPathfings[pathfindTarget.ToString()] = estimatesOfBadPathfings[pathfindTarget.ToString()] + 0.3f;
    }

    public bool IsProbablyABadPathfindTest(Vector3 pathfindTarget) {
      if (!estimatesOfBadPathfings.ContainsKey(pathfindTarget.ToString())) {
        return false;
      }

      float estimate = estimatesOfBadPathfings[pathfindTarget.ToString()];
      if (estimate < 1f) return true;

      return true;
    }

    public void Reset() {
      UnsubscribePathfinders();

      if (pathFinderMech.GameRep != null) {
        GameObject pathFinderGo = pathFinderMech.GameRep.gameObject;
        GameObject blipUnknownGo = pathFinderMech.GameRep.BlipObjectUnknown.gameObject;
        GameObject blipIdentified = pathFinderMech.GameRep.BlipObjectIdentified.gameObject;
        if (pathFinderGo) GameObject.Destroy(pathFinderGo);
        if (blipUnknownGo) GameObject.Destroy(blipUnknownGo);
        if (blipIdentified) GameObject.Destroy(blipIdentified);
      }

      if (pathFinderVehicle.GameRep != null) {
        GameObject pathFinderVehicleGo = pathFinderVehicle.GameRep.gameObject;
        GameObject vehicleBlipUnknownGo = pathFinderVehicle.GameRep.BlipObjectUnknown.gameObject;
        GameObject vehicleBlipIdentified = pathFinderVehicle.GameRep.BlipObjectIdentified.gameObject;
        if (pathFinderVehicleGo) GameObject.Destroy(pathFinderVehicleGo);
        if (vehicleBlipUnknownGo) GameObject.Destroy(vehicleBlipUnknownGo);
        if (vehicleBlipIdentified) GameObject.Destroy(vehicleBlipIdentified);
      }

      estimatesOfBadPathfings.Clear();
    }

    public void FullReset() {
      Reset();
      pathFinderMech = null;
      pathFinderVehicle = null;
    }

    private void UnsubscribePathfinders() {
      AccessTools.Method(typeof(Mech), "SubscribeMessages").Invoke(pathFinderMech, new object[] { false });
      AccessTools.Method(typeof(AbstractActor), "SubscribeMessages").Invoke(pathFinderVehicle, new object[] { false });
    }
  }
}