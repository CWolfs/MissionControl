using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Data;

using HBS.Collections;

namespace MissionControl {
  public class PathFinderManager {
    private static PathFinderManager instance;
    public static PathFinderManager Instance {
      get {
        if (instance == null) instance = new PathFinderManager();
        return instance;
      }
    }

    private static float MAX_SLOPE_FOR_PATHFINDING = 25f;

    private Mech pathFinderMech;
    private Vehicle pathFinderVehicle;

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
      Main.LogDebug("[PathFinderManager] Requesting path finder mech");
      if (pathFinderMech == null) Init();
      UnityGameInstance.BattleTechGame.DataManager.RequestNewResource(BattleTechResourceType.Prefab, pathFinderMech.MechDef.Chassis.PrefabIdentifier, null);
      UnityGameInstance.BattleTechGame.DataManager.ProcessRequests();
    }

    public void RequestPathFinderVehicle() {
      Main.LogDebug("[PathFinderManager] Requesting path finder vehicle");
      if (pathFinderVehicle == null) Init();
      UnityGameInstance.BattleTechGame.DataManager.RequestNewResource(BattleTechResourceType.Prefab, pathFinderVehicle.VehicleDef.Chassis.PrefabIdentifier, null);
      UnityGameInstance.BattleTechGame.DataManager.ProcessRequests();  
    }

    public bool IsSpawnValid(Vector3 position, Vector3 validityPosition, UnitType type) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      MapTerrainDataCell cellData = combatState.MapMetaData.GetCellAt(position);

      if (cellData.steepness > MAX_SLOPE_FOR_PATHFINDING) {
        Main.LogDebug($"[IsSpawnValid] Spawn point is too steep (> {MAX_SLOPE_FOR_PATHFINDING}). Not a valid spawn");
        return false;
      }

      TerrainMaskFlags terrainMask = cellData.terrainMask;
      bool isImpassableOrDeepWater = SplatMapInfo.IsImpassable(terrainMask) || (SplatMapInfo.IsDeepWater(terrainMask) && !cellData.MapEncounterLayerDataCell.HasBuilding);
      if (isImpassableOrDeepWater) return false;

      // Prevent any spawns outside encounter for good.
      if (!encounterLayerData.IsInEncounterBounds(position)) return false;

      AbstractActor pathfindingActor = null;
      float pathFindingZoneRadius = 25f;

      if (type == UnitType.Mech) {
        pathfindingActor = pathFinderMech;
      } else if (type == UnitType.Vehicle) {
        pathfindingActor = pathFinderVehicle;
      }

      if (pathfindingActor.GameRep == null) {
        pathfindingActor.Init(position, 0, pathfindingActor.thisUnitChecksEncounterCells);
        pathfindingActor.InitGameRep(null);
      } else {
        pathfindingActor.CurrentPosition = position;
        pathfindingActor.GameRep.transform.position = position;
        pathfindingActor.ResetPathing(false);
      }

      try {
        PathNodeGrid pathfinderPathGrid = pathfindingActor.Pathing.CurrentGrid;
        PathNode positionPathNode = pathfinderPathGrid.GetValidPathNodeAt(position, 10);
        DynamicLongRangePathfinder.PointWithCost pointWithCost = new DynamicLongRangePathfinder.PointWithCost(combatState.HexGrid.GetClosestHexPoint3OnGrid(positionPathNode.Position), (float)positionPathNode.DepthInPath, (validityPosition - positionPathNode.Position).magnitude) {
					pathNode = positionPathNode
				};
        List<Vector3> path = DynamicLongRangePathfinder.GetDynamicPathToDestination(new List<DynamicLongRangePathfinder.PointWithCost>() { pointWithCost }, validityPosition, 3000f, pathfindingActor, true, new List<AbstractActor>(), pathfindingActor.Pathing.CurrentGrid, pathFindingZoneRadius);
        
        if (path != null && path.Count > 2 && (path[path.Count - 1].DistanceFlat(validityPosition) <= pathFindingZoneRadius)) return true;

        /* // Failed attempt to improve spawn checks
        List<Vector3> path = DynamicLongRangePathfinder.GetDynamicPathToDestination(validityPosition, float.MaxValue, pathfindingActor, true, new List<AbstractActor>(), pathfindingActor.Pathing.CurrentGrid, pathFindingZoneRadius);
        if (path != null && (path[path.Count - 1].DistanceFlat(validityPosition) <= pathFindingZoneRadius)) {
          if (path.Count > 4) { // very strong pathfinding location
            return true;
          } else {
            Main.Logger.Log($"[PathFinderManager] Spawn point is valid due to proximity but is not strong enough success for pathing. Attempting to confirm.");
            CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
            List<Vector3> pointsAroundPosition = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(position, 3, 5);

            foreach (Vector3 point in pointsAroundPosition) {
              List<Vector3> secondaryPath = DynamicLongRangePathfinder.GetDynamicPathToDestination(point, float.MaxValue, pathfindingActor, true, new List<AbstractActor>(), pathfindingActor.Pathing.CurrentGrid, 2); 
              if (path != null && path.Count > 2) {
                Main.Logger.Log($"[PathFinderManager] Spawn point is valid. It is close to the validation point but can be moved away from. Success.");
                return true;
              }
            }
          }
        }
        */
      } catch (Exception) {
        Main.LogDebug($"[IsSpawnValid] Array out of bounds detected in the path finding code. Flagging as invalid spawn. Select a new spawn point.");
      }

      return false;
    }

    public void Reset() {
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
    }
  }
}