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
      Main.Logger.Log("[PathFinderManager] Requesting path finder mech");
      if (pathFinderMech == null) Init();
      UnityGameInstance.BattleTechGame.DataManager.RequestNewResource(BattleTechResourceType.Prefab, pathFinderMech.MechDef.Chassis.PrefabIdentifier, null);
      UnityGameInstance.BattleTechGame.DataManager.ProcessRequests();
    }

    public void RequestPathFinderVehicle() {
      Main.Logger.Log("[PathFinderManager] Requesting path finder vehicle");
      if (pathFinderVehicle == null) Init();
      UnityGameInstance.BattleTechGame.DataManager.RequestNewResource(BattleTechResourceType.Prefab, pathFinderVehicle.VehicleDef.Chassis.PrefabIdentifier, null);
      UnityGameInstance.BattleTechGame.DataManager.ProcessRequests();  
    }

    public bool IsSpawnValid(Vector3 position, Vector3 validityPosition, UnitType type) {
      AbstractActor pathfindingActor = null;
      float pathFindingZoneRadius = 50f;

      if (type == UnitType.Mech) {
        pathfindingActor = pathFinderMech;
      } else if (type == UnitType.Vehicle) {
        pathfindingActor = pathFinderVehicle;
      }

      if (pathfindingActor.GameRep == null) {
        CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;
        pathfindingActor.Init(position, 0, pathfindingActor.thisUnitChecksEncounterCells);
        pathfindingActor.InitGameRep(null);
      } else {
        pathfindingActor.CurrentPosition = position;
        pathfindingActor.GameRep.transform.position = position;
        pathfindingActor.ResetPathing(false);
      }

      try {
        List<Vector3> path = DynamicLongRangePathfinder.GetDynamicPathToDestination(validityPosition, float.MaxValue, pathfindingActor, true, new List<AbstractActor>(), pathfindingActor.Pathing.CurrentGrid, pathFindingZoneRadius);
        if (path != null && path.Count > 0 && (path[path.Count - 1].DistanceFlat(validityPosition) <= pathFindingZoneRadius)) return true;
      } catch (Exception) {
        Main.Logger.LogWarning($"[IsSpawnValid] Array out of bounds detected in the path finding code. Flagging as invalid spawn. Select a new spawn point.");
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