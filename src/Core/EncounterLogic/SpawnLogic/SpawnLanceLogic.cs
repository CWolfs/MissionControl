using UnityEngine;

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;

namespace MissionControl.Logic {
  public abstract class SpawnLanceLogic : SpawnLogic {
    public enum WithinOrBeyondDistanceType {
      NONE, MUST_BE_WITHIN, MUST_BE_BEYOND, BOTH
    };
    protected float minDistanceToSpawnFromInvalidSpawn = 10f;

    public SpawnLanceLogic(EncounterRules encounterRules) : base(encounterRules) { }

    protected void CorrectLanceMemberSpawns(GameObject lance) {
      Main.LogDebug($"[{this.GetType().Name}] CorrectLanceMemberSpawns to cloest hex lerped point on grid.");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");

      foreach (GameObject spawnPoint in spawnPoints) {
        Vector3 spawnPointPosition = spawnPoint.transform.position.GetClosestHexLerpedPointOnGrid();
        spawnPoint.transform.position = spawnPointPosition;
      }
    }

    protected void RunFallbackSpawn(RunPayload payload, string lanceKey, string orientationTargetKey) {
      SpawnLanceAnywhere spawnAnywhere = new SpawnLanceAnywhere(EncounterRules, lanceKey, orientationTargetKey);
      spawnAnywhere.Run(payload);
    }

    public virtual string TargetLanceKey => null;

    public virtual void RestoreOriginalLanceSpawn() {
      if (string.IsNullOrEmpty(TargetLanceKey)) return;
      if (!HasSavedOriginalPositions()) return;
      if (this.EncounterRules.ObjectLookup.TryGetValue(TargetLanceKey, out GameObject lance) && lance != null) {
        if (IsUnplacedGeneratedLance(lance)) return;
        RestoreSpawnPositions(lance);
      }
    }

    protected Vector3 ResolveVerifiedOrientationTargetPosition(GameObject orientationTarget, string keyPrefix) {
      if (orientationTarget == null) return Vector3.zero;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      Vector3 connectivityRef = (EncounterRules.SpawnerPlayerLanceGo != null && orientationTarget != EncounterRules.SpawnerPlayerLanceGo)
        ? EncounterRules.SpawnerPlayerLanceGo.transform.position.GetClosestHexLerpedPointOnGrid()
        : Vector3.zero;
      if (TryFindVerifiedAnchor(orientationTarget, keyPrefix, encounterLayerData, connectivityRef, out Vector3 verifiedAnchor)) {
        return verifiedAnchor;
      }
      return orientationTarget.transform.position.GetClosestHexLerpedPointOnGrid();
    }

    protected void HandleFallback(RunPayload payload, string lanceKey, string orientationTargetKey) {
      this.EncounterRules.ObjectLookup.TryGetValue(lanceKey, out GameObject lance);
      if (IsUnplacedGeneratedLance(lance)) {
        if (!(this is SpawnLanceAnywhere)) {
          if (lance != null) {
            RestoreSpawnPositions(lance);
          }
          Main.LogDebug($"[{this.GetType().Name}] Cannot find valid spawn. Spawning with 'SpawnAnywhere' profile.");
          RunFallbackSpawn(payload, lanceKey, orientationTargetKey);
        } else {
          Main.Logger.LogWarning($"[{this.GetType().Name}] Generated lance '{lanceKey}' exhausted fallback attempts. Attempting placement around a verified in-bounds anchor.");
          PlaceGeneratedLanceAtSafeFallback(lanceKey, orientationTargetKey);
        }
      } else if (lance != null) {
        RestoreSpawnPositions(lance);
        Main.LogDebug($"[{this.GetType().Name}] Cannot find valid spawn. Spawning at vanilla location for the encounter");
      }

      if (lance != null && lance.activeInHierarchy) {
        LookAtTarget lookAtTarget = new LookAtTarget(this.EncounterRules, lanceKey, orientationTargetKey, true);
        lookAtTarget.Run(payload);
      }
    }

    private const int MAX_RING_FALLBACK_TERRAIN_PROBES = 400;
    private const int MAX_RING_FALLBACK_PATHFIND_CHECKS = 80;
    private const int MAX_BOUNDARY_FALLBACK_TERRAIN_PROBES = 400;
    private const int MAX_BOUNDARY_FALLBACK_PATHFIND_CHECKS = 120;

    private void PlaceGeneratedLanceAtSafeFallback(string lanceKey, string orientationTargetKey) {
      if (!this.EncounterRules.ObjectLookup.TryGetValue(lanceKey, out GameObject lance) || lance == null) return;

      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      List<Vector3> candidateAnchors = new List<Vector3>();

      Vector3 actualPlayerPos = Vector3.zero;
      Vector3 validPlayerAnchor = Vector3.zero;
      bool hasValidPlayerAnchor = false;
      if (EncounterRules.SpawnerPlayerLanceGo != null) {
        actualPlayerPos = EncounterRules.SpawnerPlayerLanceGo.transform.position.GetClosestHexLerpedPointOnGrid();
        hasValidPlayerAnchor = TryFindVerifiedAnchor(EncounterRules.SpawnerPlayerLanceGo, "FallbackAnchor.PlayerLance", encounterLayerData, Vector3.zero, out validPlayerAnchor);
      }

      Vector3 verifiedPlayerTarget = hasValidPlayerAnchor ? validPlayerAnchor : actualPlayerPos;

      Vector3 actualOrientationPos = Vector3.zero;
      Vector3 validOrientationAnchor = Vector3.zero;
      bool hasValidOrientationAnchor = false;
      if (this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out GameObject orientationTarget) && orientationTarget != null) {
        actualOrientationPos = orientationTarget.transform.position.GetClosestHexLerpedPointOnGrid();
        hasValidOrientationAnchor = TryFindVerifiedAnchor(orientationTarget, $"FallbackAnchor.{orientationTarget.name}", encounterLayerData, verifiedPlayerTarget, out validOrientationAnchor);
        if (hasValidOrientationAnchor) {
          candidateAnchors.Add(validOrientationAnchor);
        }
      }

      if (hasValidPlayerAnchor && !candidateAnchors.Contains(validPlayerAnchor)) {
        candidateAnchors.Add(validPlayerAnchor);
      }

      Vector3 verifiedOrientationTarget = hasValidOrientationAnchor ? validOrientationAnchor : actualOrientationPos;
      bool isPlayerLance = IsLancePlayerLance(lanceKey);
      Vector3 reachabilityTarget = isPlayerLance
        ? (verifiedOrientationTarget != Vector3.zero ? verifiedOrientationTarget : verifiedPlayerTarget)
        : (verifiedPlayerTarget != Vector3.zero ? verifiedPlayerTarget : verifiedOrientationTarget);
      Vector3 secondaryReachabilityTarget = (!isPlayerLance && hasValidOrientationAnchor && validOrientationAnchor != reachabilityTarget)
        ? validOrientationAnchor
        : Vector3.zero;

      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");
      if (spawnPoints.Count == 0) return;

      Vector3 offMapStaging = new Vector3(10000f, 0f, 10000f);
      bool committed = false;

      try {
        // Deterministic, strictly budgeted search over nearest-first anchor rings and evenly distributed axial boundary hexes
        // Move generated lance members off-map while probing so vacated placeholder transforms do not block sibling placement
        StageLanceOffMap(lance, spawnPoints, offMapStaging);

        EncounterBoundaryChunkGameLogic chunkBoundary = MissionControl.Instance.EncounterLayerGameObject.GetComponentInChildren<EncounterBoundaryChunkGameLogic>();
        Rect boundaryRect = default(Rect);
        bool hasBoundaryRect = (chunkBoundary != null);
        if (hasBoundaryRect) {
          boundaryRect = chunkBoundary.GetEncounterBoundaryRectBounds();
        }

        List<Vector3> scanCenters = new List<Vector3>();
        foreach (Vector3 anchor in candidateAnchors) {
          if (!scanCenters.Contains(anchor)) scanCenters.Add(anchor);
        }
        if (reachabilityTarget != Vector3.zero && !scanCenters.Contains(reachabilityTarget)) {
          scanCenters.Add(reachabilityTarget);
        }
        if (hasBoundaryRect) {
          Vector3 boundaryCenter = new Vector3(boundaryRect.center.x, 0f, boundaryRect.center.y).GetClosestHexLerpedPointOnGrid();
          if (!scanCenters.Contains(boundaryCenter)) scanCenters.Add(boundaryCenter);
        }

        int nextMemberIdx = 0;
        HashSet<Vector3> visitedHexes = new HashSet<Vector3>();
        int ringTerrainProbeBudget = MAX_RING_FALLBACK_TERRAIN_PROBES;
        int ringPathfindBudget = MAX_RING_FALLBACK_PATHFIND_CHECKS;

        for (int centerIdx = 0; centerIdx < scanCenters.Count; centerIdx++) {
          if (ringTerrainProbeBudget <= 0 || ringPathfindBudget <= 0) break;

          Vector3 center = scanCenters[centerIdx];
          int centersRemaining = scanCenters.Count - centerIdx;
          int centerTerrainBudget = Math.Max(1, ringTerrainProbeBudget / centersRemaining);
          int centerPathfindBudget = Math.Max(1, ringPathfindBudget / centersRemaining);
          int terrainBefore = centerTerrainBudget;
          int pathfindBefore = centerPathfindBudget;

          List<Vector3> ringHexes = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(center, 20);
          ringHexes.Sort((a, b) => (a - center).sqrMagnitude.CompareTo((b - center).sqrMagnitude));
          bool done = TryCollectFallbackHexes(combatState, encounterLayerData, spawnPoints, ref nextMemberIdx, ringHexes, visitedHexes, reachabilityTarget, secondaryReachabilityTarget, ref centerTerrainBudget, ref centerPathfindBudget);
          ringTerrainProbeBudget -= (terrainBefore - centerTerrainBudget);
          ringPathfindBudget -= (pathfindBefore - centerPathfindBudget);
          if (done) {
            SetLanceRootPreservingChildren(lance, spawnPoints[0].transform.position, spawnPoints);
            committed = true;
            return;
          }
        }

        if (hasBoundaryRect) {
          List<Vector3> allBoundaryHexes = EnumerateBoundaryHexes(combatState.HexGrid, boundaryRect);
          if (allBoundaryHexes.Count > 0) {
            int terrainProbeBudget = MAX_BOUNDARY_FALLBACK_TERRAIN_PROBES;
            int boundaryPathfindBudget = MAX_BOUNDARY_FALLBACK_PATHFIND_CHECKS;
            int step = PickCoprimeStep(allBoundaryHexes.Count);
            int idx = 0;

            for (int probed = 0; probed < allBoundaryHexes.Count && terrainProbeBudget > 0 && boundaryPathfindBudget > 0 && nextMemberIdx < spawnPoints.Count; probed++) {
              Vector3 hex = allBoundaryHexes[idx].GetClosestHexLerpedPointOnGrid();
              idx = (idx + step) % allBoundaryHexes.Count;

              if (!visitedHexes.Add(hex)) continue;
              if (!encounterLayerData.IsInEncounterBounds(hex)) continue;

              terrainProbeBudget--;
              if (!PathFinderManager.Instance.IsCellValidDrySpawn(combatState, hex)) continue;

              GameObject spawnPoint = spawnPoints[nextMemberIdx];
              if (hex.IsTooCloseToAnotherSpawn(spawnPoint)) continue;

              if (!IsFallbackHexReachable(spawnPoint, hex, reachabilityTarget, secondaryReachabilityTarget, ref boundaryPathfindBudget)) continue;

              spawnPoint.transform.position = hex;
              nextMemberIdx++;

              if (nextMemberIdx < spawnPoints.Count && terrainProbeBudget > 0 && boundaryPathfindBudget > 0) {
                // Once we find a reachable landmass, check immediate surrounding hexes first for the rest of the lance
                int localTerrainBudget = Math.Min(terrainProbeBudget, 60);
                int localPathfindBudget = Math.Min(boundaryPathfindBudget, 24);
                List<Vector3> localHexes = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(hex, 5);
                localHexes.Sort((a, b) => (a - hex).sqrMagnitude.CompareTo((b - hex).sqrMagnitude));
                int terrainBefore = localTerrainBudget;
                int pathfindBefore = localPathfindBudget;
                bool done = TryCollectFallbackHexes(combatState, encounterLayerData, spawnPoints, ref nextMemberIdx, localHexes, visitedHexes, reachabilityTarget, secondaryReachabilityTarget, ref localTerrainBudget, ref localPathfindBudget);
                terrainProbeBudget -= (terrainBefore - localTerrainBudget);
                boundaryPathfindBudget -= (pathfindBefore - localPathfindBudget);
                if (done) {
                  SetLanceRootPreservingChildren(lance, spawnPoints[0].transform.position, spawnPoints);
                  committed = true;
                  return;
                }
              }
            }

            if (nextMemberIdx >= spawnPoints.Count) {
              SetLanceRootPreservingChildren(lance, spawnPoints[0].transform.position, spawnPoints);
              committed = true;
              return;
            }
          }
        }

        Main.Logger.LogError($"[{this.GetType().Name}] Could not place generated lance '{lanceKey}' on distinct reachable in-bounds hexes ({nextMemberIdx}/{spawnPoints.Count} placed).");
      } finally {
        if (!committed) {
          RestoreSpawnPositions(lance);
        }
      }
    }

    private bool IsFallbackHexReachable(
      GameObject spawnPoint,
      Vector3 hex,
      Vector3 primaryTarget,
      Vector3 secondaryTarget,
      ref int remainingPathfindBudget
    ) {
      if (remainingPathfindBudget <= 0) return false;
      remainingPathfindBudget--;
      if (!PathFinderManager.Instance.IsSpawnValid(spawnPoint, hex, primaryTarget, UnitType.Mech, spawnPoint.name)) {
        return false;
      }

      if (secondaryTarget != Vector3.zero) {
        if (remainingPathfindBudget <= 0) return false;
        remainingPathfindBudget--;
        if (!PathFinderManager.Instance.IsSpawnValid(spawnPoint, hex, secondaryTarget, UnitType.Mech, spawnPoint.name)) {
          return false;
        }
      }

      return true;
    }

    private bool TryFindVerifiedAnchor(GameObject targetGo, string keyPrefix, EncounterLayerData encounterLayerData, Vector3 connectivityReference, out Vector3 verifiedAnchor) {
      verifiedAnchor = Vector3.zero;
      if (targetGo == null) return false;

      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      // For a lance, prefer an actual child spawn point that is on the walkable path grid with valid neighbours (not an isolated ledge)
      List<GameObject> childSpawnPoints = targetGo.FindAllContains("SpawnPoint");
      Vector3 firstWalkableCandidate = Vector3.zero;
      bool hasWalkableCandidate = false;

      foreach (GameObject child in childSpawnPoints) {
        Vector3 childPos = child.transform.position.GetClosestHexLerpedPointOnGrid();
        if (!encounterLayerData.IsInEncounterBounds(childPos)) continue;
        if (!PathFinderManager.Instance.HasValidPathNodeAndNeighbours(combatState, childPos, UnitType.Mech)) continue;

        if (!hasWalkableCandidate) {
          firstWalkableCandidate = childPos;
          hasWalkableCandidate = true;
        }
        if (connectivityReference == Vector3.zero || PathFinderManager.Instance.IsSpawnValid(child, childPos, connectivityReference, UnitType.Mech, keyPrefix)) {
          verifiedAnchor = childPos;
          return true;
        }
      }

      // Check the target's own snapped hex directly
      Vector3 rootPos = targetGo.transform.position.GetClosestHexLerpedPointOnGrid();
      if (encounterLayerData.IsInEncounterBounds(rootPos) && PathFinderManager.Instance.HasValidPathNodeAndNeighbours(combatState, rootPos, UnitType.Mech)) {
        if (!hasWalkableCandidate) {
          firstWalkableCandidate = rootPos;
          hasWalkableCandidate = true;
        }
        if (connectivityReference == Vector3.zero || PathFinderManager.Instance.IsSpawnValid(targetGo, rootPos, connectivityReference, UnitType.Mech, keyPrefix)) {
          verifiedAnchor = rootPos;
          return true;
        }
      }

      if (hasWalkableCandidate) {
        verifiedAnchor = firstWalkableCandidate;
        return true;
      }

      // If the target itself sits on a non-walkable footprint (e.g. a large base plot whose centre is >25m inside impassable buildings),
      // find the nearest walkable perimeter hex independently of the blocked centre, preferring one connected to the playable landmass
      Vector3 referenceTarget = (childSpawnPoints.Count > 0)
        ? childSpawnPoints[0].transform.position.GetClosestHexLerpedPointOnGrid()
        : rootPos;

      List<Vector3> nearbyHexes = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(referenceTarget, 5);
      nearbyHexes.Sort((a, b) => (a - referenceTarget).sqrMagnitude.CompareTo((b - referenceTarget).sqrMagnitude));
      int perimeterChecks = 0;
      foreach (Vector3 rawHex in nearbyHexes) {
        if (perimeterChecks >= 30) break;
        Vector3 hex = rawHex.GetClosestHexLerpedPointOnGrid();
        if (!encounterLayerData.IsInEncounterBounds(hex)) continue;
        if (!PathFinderManager.Instance.IsCellValidDrySpawn(combatState, hex)) continue;

        perimeterChecks++;
        if (!PathFinderManager.Instance.HasValidPathNodeAndNeighbours(combatState, hex, UnitType.Mech)) continue;

        if (!hasWalkableCandidate) {
          firstWalkableCandidate = hex;
          hasWalkableCandidate = true;
        }
        if (connectivityReference == Vector3.zero || PathFinderManager.Instance.IsSpawnValid(targetGo, hex, connectivityReference, UnitType.Mech, keyPrefix)) {
          verifiedAnchor = hex;
          return true;
        }
      }

      if (hasWalkableCandidate) {
        verifiedAnchor = firstWalkableCandidate;
        return true;
      }

      return false;
    }

    private void StageLanceOffMap(GameObject lance, List<GameObject> spawnPoints, Vector3 offMapStaging) {
      lance.transform.position = offMapStaging;
      foreach (GameObject sp in spawnPoints) {
        sp.transform.position = offMapStaging;
      }
    }

    private bool TryCollectFallbackHexes(
      CombatGameState combatState,
      EncounterLayerData encounterLayerData,
      List<GameObject> spawnPoints,
      ref int nextMemberIdx,
      IEnumerable<Vector3> candidatePoints,
      HashSet<Vector3> visitedHexes,
      Vector3 reachabilityTarget,
      Vector3 secondaryReachabilityTarget,
      ref int remainingTerrainProbeBudget,
      ref int remainingPathfindBudget
    ) {
      foreach (Vector3 rawPoint in candidatePoints) {
        if (nextMemberIdx >= spawnPoints.Count) return true;
        if (remainingTerrainProbeBudget <= 0 || remainingPathfindBudget <= 0) return false;

        Vector3 hex = rawPoint.GetClosestHexLerpedPointOnGrid();
        if (!visitedHexes.Add(hex)) continue;
        if (!encounterLayerData.IsInEncounterBounds(hex)) continue;

        remainingTerrainProbeBudget--;
        if (!PathFinderManager.Instance.IsCellValidDrySpawn(combatState, hex)) continue;

        GameObject spawnPoint = spawnPoints[nextMemberIdx];
        if (hex.IsTooCloseToAnotherSpawn(spawnPoint)) continue;

        if (!IsFallbackHexReachable(spawnPoint, hex, reachabilityTarget, secondaryReachabilityTarget, ref remainingPathfindBudget)) continue;

        spawnPoint.transform.position = hex;
        nextMemberIdx++;
      }

      return nextMemberIdx >= spawnPoints.Count;
    }

    private List<Vector3> EnumerateBoundaryHexes(HexGrid hexGrid, Rect boundaryRect) {
      List<Vector3> hexes = new List<Vector3>();
      Vector2 c0 = hexGrid.CartesianToHexAxial(new Vector3(boundaryRect.xMin, 0f, boundaryRect.yMin));
      Vector2 c1 = hexGrid.CartesianToHexAxial(new Vector3(boundaryRect.xMin, 0f, boundaryRect.yMax));
      Vector2 c2 = hexGrid.CartesianToHexAxial(new Vector3(boundaryRect.xMax, 0f, boundaryRect.yMin));
      Vector2 c3 = hexGrid.CartesianToHexAxial(new Vector3(boundaryRect.xMax, 0f, boundaryRect.yMax));

      int minQ = Mathf.FloorToInt(Mathf.Min(Mathf.Min(c0.x, c1.x), Mathf.Min(c2.x, c3.x))) - 1;
      int maxQ = Mathf.CeilToInt(Mathf.Max(Mathf.Max(c0.x, c1.x), Mathf.Max(c2.x, c3.x))) + 1;
      int minR = Mathf.FloorToInt(Mathf.Min(Mathf.Min(c0.y, c1.y), Mathf.Min(c2.y, c3.y))) - 1;
      int maxR = Mathf.CeilToInt(Mathf.Max(Mathf.Max(c0.y, c1.y), Mathf.Max(c2.y, c3.y))) + 1;

      for (int q = minQ; q <= maxQ; q++) {
        for (int r = minR; r <= maxR; r++) {
          Vector3 worldPos = hexGrid.HexAxialToCartesian(new Vector2(q, r));
          if (boundaryRect.Contains(new Vector2(worldPos.x, worldPos.z))) {
            hexes.Add(worldPos);
          }
        }
      }
      return hexes;
    }

    private int PickCoprimeStep(int count) {
      if (count <= 2) return 1;
      int step = Math.Max(1, (int)(count * 0.61803398875f));
      while (Gcd(step, count) != 1) {
        step++;
        if (step >= count) step = 1;
      }
      return step;
    }

    private int Gcd(int a, int b) {
      while (b != 0) {
        int t = a % b;
        a = b;
        b = t;
      }
      return Math.Abs(a);
    }

    private void SetLanceRootPreservingChildren(GameObject lance, Vector3 rootPosition, List<GameObject> spawnPoints) {
      List<Vector3> worldPositions = new List<Vector3>(spawnPoints.Count);
      for (int i = 0; i < spawnPoints.Count; i++) {
        worldPositions.Add(spawnPoints[i].transform.position);
      }
      lance.transform.position = rootPosition;
      for (int i = 0; i < spawnPoints.Count; i++) {
        spawnPoints[i].transform.position = worldPositions[i];
      }
    }

    protected bool AreLanceMemberSpawnsValid(GameObject lance, Vector3 checkTarget) {
      List<GameObject> invalidLanceSpawns = GetInvalidLanceMemberSpawns(lance, checkTarget);
      if (invalidLanceSpawns.Count <= 0) return true;
      return false;
    }

    protected List<GameObject> GetInvalidLanceMemberSpawns(GameObject lance, Vector3 checkTarget) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;

      List<GameObject> invalidLanceSpawns = new List<GameObject>();
      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");
      Vector3 checkTargetPosition = checkTarget.GetClosestHexLerpedPointOnGrid();

      foreach (GameObject spawnPoint in spawnPoints) {
        if (!TryGetClosestValidPathFindingHex(spawnPoint, spawnPoint.transform.position, $"GetInvalidLanceMemberSpawns.{spawnPoint.name}", IsLancePlayerLance(lance.name) ? checkTargetPosition : Vector3.zero, out Vector3 spawnPointPosition, 2)) {
          Main.LogDebugWarning($"[SpawnLanceLogic.GetInvalidLanceMemberSpawns] Could not find valid pathfinding hex for '{spawnPoint.name}'. Select a new lance spawn point.");
          invalidLanceSpawns.Add(spawnPoint);
          continue;
        }
        Main.LogDebug($"[SpawnLanceLogic.GetInvalidLanceMemberSpawns] Spawn point's closest hex lerped point on grid for '{spawnPoint.name}' is '{spawnPointPosition}'");

        if (!encounterLayerData.IsInEncounterBounds(spawnPointPosition)) {
          Main.LogDebugWarning("[SpawnLanceLogic.GetInvalidLanceMemberSpawns] Lance member spawn is outside of the boundary. Select a new lance spawn point.");
          invalidLanceSpawns.Add(spawnPoint);
          continue;
        }

        // Ensure the lance member's spawn's closest valid point isn't on another spawn point's closest valid point
        if (IsPointTooCloseToOtherPointsClosestPointOnGrid(spawnPointPosition, spawnPoints.Where(sp => spawnPoint.name != sp.name).ToList())) {
          Main.LogDebugWarning("[SpawnLanceLogic.GetInvalidLanceMemberSpawns] Lance member spawn is too close to the other spawns when snapped to the grid");
          invalidLanceSpawns.Add(spawnPoint);
          continue;
        }

        if (!PathFinderManager.Instance.IsSpawnValid(spawnPoint, spawnPointPosition, checkTargetPosition, UnitType.Mech, spawnPoint.name)) {
          Main.LogDebugWarning($"[SpawnLanceLogic.GetInvalidLanceMemberSpawns] Lance member spawn '{spawnPoint.name}' path to check target '{checkTarget}' is blocked. Select a new lance spawn point");
          invalidLanceSpawns.Add(spawnPoint);
          continue;
        }

        spawnPoint.transform.position = spawnPointPosition;
      }

      return invalidLanceSpawns;
    }

    public bool IsObjectSpawnValid(GameObject spawnPoint, Vector3 checkTarget) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      Vector3 checkTargetPosition = checkTarget.GetClosestHexLerpedPointOnGrid();

      if (!TryGetClosestValidPathFindingHex(spawnPoint, spawnPoint.transform.position, $"IsObjectSpawnInvalid.{spawnPoint.name}", Vector3.zero, out Vector3 spawnPointPosition, 2)) {
        Main.LogDebugWarning($"[SpawnLanceLogic.IsObjectSpawnInvalid] Could not find valid pathfinding hex for '{spawnPoint.name}'. Select a new object spawn point.");
        return false;
      }
      Main.LogDebug($"[SpawnLanceLogic.IsObjectSpawnInvalid] Object spawn point's closest hex lerped point on grid for '{spawnPoint.name}' is '{spawnPointPosition}'");

      if (!encounterLayerData.IsInEncounterBounds(spawnPointPosition)) {
        Main.LogDebugWarning("[SpawnLanceLogic.IsObjectSpawnInvalid] Object spawn is outside of the boundary. Select a new object spawn point.");
        return false;
      }

      if (!PathFinderManager.Instance.IsSpawnValid(spawnPoint, spawnPointPosition, checkTargetPosition, UnitType.Mech, spawnPoint.name)) {
        Main.LogDebugWarning($"[SpawnLanceLogic.IsObjectSpawnInvalid] Object spawn '{spawnPoint.name}' path to check target '{checkTarget}' is blocked. Select a new object spawn point");
        return false;
      }

      spawnPoint.transform.position = spawnPointPosition;
      return true;
    }

    protected bool IsPointTooCloseToOtherPointsClosestPointOnGrid(Vector3 point, List<GameObject> points) {
      List<Vector3> vectorPoints = new List<Vector3>();
      for (int i = 0; i < points.Count; i++) {
        vectorPoints.Add(points[i].transform.position);
      }
      return IsPointTooCloseToOtherPointsClosestPointOnGrid(point, vectorPoints);
    }

    protected bool IsPointTooCloseToOtherPointsClosestPointOnGrid(Vector3 point, List<Vector3> points) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      foreach (Vector3 checkPoint in points) {
        Vector3 validCheckPoint = combatState.HexGrid.GetClosestPointOnGrid(checkPoint);
        Vector3 vectorToCheckPoint = point - validCheckPoint;
        vectorToCheckPoint.y = 0;
        float distanceBetweenPoints = vectorToCheckPoint.magnitude;
        if (distanceBetweenPoints < minDistanceToSpawnFromInvalidSpawn) return true;
      }
      return false;
    }

    public bool IsLancePlayerLance(string lanceKey) {
      if (lanceKey.Contains("Player")) {
        return true;
      }
      return false;
    }
  }
}