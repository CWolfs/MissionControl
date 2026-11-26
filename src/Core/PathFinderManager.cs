using UnityEngine;
using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Rendering.UrbanWarfare;

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
    private const float MAX_TERRAIN_HEIGHT_MISMATCH = 1.0f;
    private const float BUILDING_SAMPLE_OFFSET_DIAG = 2.13f; // 1.5m X/Z building raycast offset in MapMetaDataExporter
    private const float MAX_OVERHEAD_CLEARANCE = 15f;
    private static bool layersInitialised = false;
    private static int terrainLayer = -1;
    private static int obstructionLayer = -1;
    private static int noCollisionLayer = -1;
    private static int groundRaycastMask = Physics.DefaultRaycastLayers;

    public static int GroundRaycastMask {
      get {
        EnsureLayersInitialised();
        return groundRaycastMask;
      }
    }

    private static void EnsureLayersInitialised() {
      if (layersInitialised) return;
      terrainLayer = LayerMask.NameToLayer("Terrain");
      obstructionLayer = LayerMask.NameToLayer("Obstruction");
      noCollisionLayer = LayerMask.NameToLayer("NoCollision");
      groundRaycastMask = Physics.DefaultRaycastLayers;
      if (noCollisionLayer >= 0) {
        groundRaycastMask &= ~(1 << noCollisionLayer);
      }
      layersInitialised = true;
    }

    private Mech pathFinderMech;
    // private Vehicle pathFinderVehicle;

    private Dictionary<string, float> estimatesOfBadPathfings = new Dictionary<string, float>();

    private PathFinderManager() {
      Init();
    }

    public void Init() {
      pathFinderMech = CreatePathFinderMech();
      // pathFinderVehicle = CreatePathFindingVehicle();
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

    /*
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
    */

    public void RequestPathFinderMech() {
      Main.LogDebug("[PFM] Requesting path finder mech");
      if (pathFinderMech == null) Init();
      DataManager.Instance.RequestResourcesAndProcess(BattleTechResourceType.Prefab, pathFinderMech.MechDef.Chassis.PrefabIdentifier);
    }

    /*
    public void RequestPathFinderVehicle() {
      Main.LogDebug("[PFM] Requesting path finder vehicle");
      if (pathFinderVehicle == null) Init();
      DataManager.Instance.RequestResourcesAndProcess(BattleTechResourceType.Prefab, pathFinderVehicle.VehicleDef.Chassis.PrefabIdentifier);
    }
    */

    private AbstractActor GetPathFindingActor(UnitType type) {
      if (type == UnitType.Mech) {
        return pathFinderMech;
        // } else if (type == UnitType.Vehicle) {
        //   return pathFinderVehicle;
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

      if (IsCellImpassableOrDeepWater(combatState, cellData, allowTraversableStructureOverWater: false)) return false;
      if (!encounterLayerData.IsInEncounterBounds(position)) return false;
      if (cellData.cachedHeight > (cellData.terrainHeight + 50f)) return false;
      if (HasTerrainHeightMismatch(combatState, cellData, position)) return false;

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
        if (IsCellImpassableOrDeepWater(combatState, cellData, allowTraversableStructureOverWater: true)) {
          Main.LogDebug("[PFM.HasPathImpassableOrDeepWaterTiles] Path has impassable or deep water tiles in it");
          return true;
        }
      }

      return false;
    }

    public static bool IsValidGroundCollider(Collider hitCollider, out bool isTerrainHit) {
      isTerrainHit = false;
      if (hitCollider == null) return false;

      EnsureLayersInitialised();

      int layer = hitCollider.gameObject.layer;
      if (noCollisionLayer >= 0 && layer == noCollisionLayer) return false;

      if (hitCollider.GetComponentInParent<PilotableActorRepresentation>() != null) return false;

      DestructibleObject destructible = hitCollider.GetComponentInParent<DestructibleObject>();
      if (destructible != null && destructible.isFlimsy) return false;

      if (hitCollider.GetComponentInParent<DestructibleUrbanFlimsy>() != null) return false;
      if (IsUnderFlimsyGroup(hitCollider.transform)) return false;

      isTerrainHit = hitCollider.GetComponentInParent<Terrain>() != null || (terrainLayer >= 0 && layer == terrainLayer);
      if (isTerrainHit) return true;

      if (hitCollider.GetComponentInParent<ObstructionGameLogic>() != null) return true;

      return obstructionLayer >= 0 && layer == obstructionLayer;
    }

    private static bool IsUnderFlimsyGroup(Transform transform) {
      Transform current = transform;
      while (current != null) {
        if (string.Equals(current.name, "_flimsy", StringComparison.Ordinal)) return true;
        current = current.parent;
      }
      return false;
    }

    private bool HasTerrainHeightMismatch(CombatGameState combatState, MapTerrainDataCell cellData, Vector3 position) {
      float mapMetaHeight = combatState.MapMetaData.GetLerpedHeightAt(position);
      bool hasBuilding = cellData.MapEncounterLayerDataCell != null && cellData.MapEncounterLayerDataCell.HasBuilding;
      float maxMismatch = MAX_TERRAIN_HEIGHT_MISMATCH + (hasBuilding
        ? BUILDING_SAMPLE_OFFSET_DIAG * Mathf.Tan(Mathf.Clamp(cellData.cachedSteepness, 0f, MAX_SLOPE_FOR_PATHFINDING) * Mathf.Deg2Rad)
        : 0f);

      float? liveGroundHeight = GetSupportingGroundHeight(position, mapMetaHeight);
      if (!liveGroundHeight.HasValue) {
        Main.LogDebug($"[PFM.HasTerrainHeightMismatch] No supporting ground collider found for MapMetaData height '{mapMetaHeight}' at '{position}'. Rejecting spawn.");
        return true;
      }

      float heightDelta = Mathf.Abs(liveGroundHeight.Value - mapMetaHeight);
      if (heightDelta > maxMismatch) {
        Main.LogDebug($"[PFM.HasTerrainHeightMismatch] Live ground height '{liveGroundHeight.Value}' differs from MapMetaData height '{mapMetaHeight}' by '{heightDelta}' (> {maxMismatch}) at '{position}'. Rejecting spawn.");
        return true;
      }

      if (HasOverheadObstructionOverlap(position, liveGroundHeight.Value)) {
        Main.LogDebug($"[PFM.HasTerrainHeightMismatch] Solid obstruction overlaps unit clearance above '{liveGroundHeight.Value}' at '{position}'. Rejecting spawn.");
        return true;
      }

      return false;
    }

    private float? GetSupportingGroundHeight(Vector3 position, float referenceHeight) {
      float rayOriginY = referenceHeight + SnapToTerrain.terrainDistance;
      Vector3 rayOrigin = new Vector3(position.x, rayOriginY, position.z);
      RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, SnapToTerrain.terrainDistance * 2f, GroundRaycastMask, QueryTriggerInteraction.Ignore);

      float? highestSupportY = null;
      for (int i = 0; i < hits.Length; i++) {
        Collider hitCollider = hits[i].collider;
        if (!IsValidGroundCollider(hitCollider, out bool isTerrainHit)) continue;
        if (!isTerrainHit
            && hits[i].point.y > referenceHeight + MAX_OVERHEAD_CLEARANCE
            && HasOpenClearanceBelow(hitCollider, position, referenceHeight, hits[i].point.y)) {
          continue;
        }

        if (!highestSupportY.HasValue || hits[i].point.y > highestSupportY.Value) {
          highestSupportY = hits[i].point.y;
        }
      }

      return highestSupportY;
    }

    private bool HasOpenClearanceBelow(Collider roofCollider, Vector3 position, float referenceHeight, float roofY) {
      float startY = referenceHeight + 0.5f;
      float distance = roofY - startY;
      if (distance <= MAX_OVERHEAD_CLEARANCE) return false;

      Ray upRay = new Ray(new Vector3(position.x, startY, position.z), Vector3.up);
      if (roofCollider.Raycast(upRay, out RaycastHit undersideHit, distance)) {
        return undersideHit.point.y >= referenceHeight + MAX_OVERHEAD_CLEARANCE;
      }

      return false;
    }

    private bool HasOverheadObstructionOverlap(Vector3 position, float groundHeight) {
      const float capsuleRadius = 1.5f;
      Vector3 bottom = new Vector3(position.x, groundHeight + 3.0f, position.z);
      Vector3 top = new Vector3(position.x, groundHeight + MAX_OVERHEAD_CLEARANCE - capsuleRadius, position.z);
      Collider[] overlaps = Physics.OverlapCapsule(bottom, top, capsuleRadius, GroundRaycastMask, QueryTriggerInteraction.Ignore);

      for (int i = 0; i < overlaps.Length; i++) {
        if (!IsValidGroundCollider(overlaps[i], out bool isTerrainHit)) continue;
        if (isTerrainHit) continue;
        return true;
      }

      return false;
    }

    public bool IsCellValidDrySpawn(CombatGameState combatState, Vector3 position) {
      MapTerrainDataCell cellData = combatState.MapMetaData.GetCellAt(position);
      if (cellData.cachedSteepness > MAX_SLOPE_FOR_PATHFINDING) return false;
      if (IsCellImpassableOrDeepWater(combatState, cellData, allowTraversableStructureOverWater: false)) return false;
      if (cellData.cachedHeight > (cellData.terrainHeight + 50f)) return false;
      if (HasTerrainHeightMismatch(combatState, cellData, position)) return false;
      return true;
    }

    public bool HasValidPathNodeAndNeighbours(CombatGameState combatState, Vector3 position, UnitType type) {
      if (!IsCellValidDrySpawn(combatState, position)) return false;

      AbstractActor pathfindingActor = GetPathFindingActor(type);
      if (pathfindingActor == null) return false;

      try {
        SetupPathfindingActor(position, pathfindingActor);
        PathNodeGrid pathfinderPathGrid = pathfindingActor.Pathing.CurrentGrid;
        PathNode positionPathNode = pathfinderPathGrid.GetValidPathNodeAt(position, pathfindingActor.Pathing.MaxCost);
        if (positionPathNode == null) {
          Reset();
          return false;
        }
        bool hasNeighbours = HasValidNeighbours(positionPathNode, position, type);
        Reset();
        return hasNeighbours;
      } catch (Exception e) {
        Main.LogDebug($"[PFM.HasValidPathNodeAndNeighbours] Caught error at '{position}': {e.Message}");
        Reset();
        return false;
      }
    }

    private bool IsCellImpassableOrDeepWater(CombatGameState combatState, MapTerrainDataCell cellData, bool allowTraversableStructureOverWater) {
      TerrainMaskFlags terrainMask = cellData.terrainMask;
      bool hasBuilding = cellData.MapEncounterLayerDataCell != null && cellData.MapEncounterLayerDataCell.HasBuilding;
      BuildingRaycastHit topmostBuilding = hasBuilding ? cellData.MapEncounterLayerDataCell.GetTopmostBuilding() : null;

      ObstructionGameLogic obstruction = null;
      TerrainMaskFlags buildingMask = TerrainMaskFlags.None;
      if (topmostBuilding != null) {
        obstruction = topmostBuilding.obstructionGameLogic;
        if (obstruction == null && combatState?.ItemRegistry != null && !string.IsNullOrEmpty(topmostBuilding.buildingGuid)) {
          obstruction = combatState.ItemRegistry.GetItemByGUID<ObstructionGameLogic>(topmostBuilding.buildingGuid);
        }
        buildingMask = (obstruction != null) ? obstruction.TerrainMask : topmostBuilding.BuildingTerrainMask;
      }

      bool hasTraversableStructureOverWater = allowTraversableStructureOverWater
        && obstruction != null
        && IsTraversableStructureOverWater(obstruction, buildingMask);

      bool isImpassableOrDeepWater = SplatMapInfo.IsImpassable(terrainMask)
        || SplatMapInfo.IsImpassable(buildingMask)
        || SplatMapInfo.IsMapBoundary(terrainMask)
        || (SplatMapInfo.IsDeepWater(terrainMask) && !hasTraversableStructureOverWater)
        || SplatMapInfo.IsDeepWater(buildingMask);

      if (isImpassableOrDeepWater) {
        Main.LogDebug("[PFM.IsCellImpassableOrDeepWater] Tile is impassable, map boundary, or deep water");
        return true;
      }
      return false;
    }

    private bool IsTraversableStructureOverWater(ObstructionGameLogic obstruction, TerrainMaskFlags buildingMask) {
      if (SplatMapInfo.IsImpassable(buildingMask) || SplatMapInfo.IsDeepWater(buildingMask)) return false;
      if (obstruction.isBridge) return true;
      if (obstruction.buildingDefId == ObstructionGameLogic.buildingDef_SolidObstruction) return false;
      return !obstruction.IsRealBuilding;
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
      if (estimate < 1f) return false;

      return true;
    }

    public void Reset() {
      UnsubscribePathfinders();

      if (pathFinderMech != null && pathFinderMech.GameRep != null) {
        GameObject pathFinderGo = pathFinderMech.GameRep.gameObject;
        GameObject blipUnknownGo = pathFinderMech.GameRep.BlipObjectUnknown.gameObject;
        GameObject blipIdentified = pathFinderMech.GameRep.BlipObjectIdentified.gameObject;
        if (pathFinderGo) GameObject.Destroy(pathFinderGo);
        if (blipUnknownGo) GameObject.Destroy(blipUnknownGo);
        if (blipIdentified) GameObject.Destroy(blipIdentified);
      }

      // if (pathFinderVehicle != null && pathFinderVehicle.GameRep != null) {
      //   GameObject pathFinderVehicleGo = pathFinderVehicle.GameRep.gameObject;
      //   GameObject vehicleBlipUnknownGo = pathFinderVehicle.GameRep.BlipObjectUnknown.gameObject;
      //   GameObject vehicleBlipIdentified = pathFinderVehicle.GameRep.BlipObjectIdentified.gameObject;
      //   if (pathFinderVehicleGo) GameObject.Destroy(pathFinderVehicleGo);
      //   if (vehicleBlipUnknownGo) GameObject.Destroy(vehicleBlipUnknownGo);
      //   if (vehicleBlipIdentified) GameObject.Destroy(vehicleBlipIdentified);
      // }

      estimatesOfBadPathfings.Clear();
    }

    public void FullReset() {
      pathFinderMech = null;
      // pathFinderVehicle = null;
    }

    private void UnsubscribePathfinders() {
      if (pathFinderMech != null) AccessTools.Method(typeof(AbstractActor), "SubscribeMessages").Invoke(pathFinderMech, new object[] { false });
      /// if (pathFinderVehicle != null) AccessTools.Method(typeof(AbstractActor), "SubscribeMessages").Invoke(pathFinderVehicle, new object[] { false });
    }
  }
}