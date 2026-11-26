using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class SpawnObjectsAroundTarget : SpawnLanceLogic {
    private LogicState state;
    private bool onlyRepositionIfInvalid = false;

    private List<string> configuredObjectKeys = new List<string>();
    private List<string> objectKeys = new List<string>();
    private List<string> orientationTargetKeys = new List<string>();
    private Dictionary<string, string> keyLookup = new Dictionary<string, string>();
    private string defaultOrientationTargetKey = "";

    private List<GameObject> objectGos = new List<GameObject>();
    private Dictionary<string, GameObject> orientationTargets = new Dictionary<string, GameObject>();
    private Dictionary<GameObject, Vector3> validOrientationTargetPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Vector3> cachedSpawnSnappedPositions = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, Vector3> cachedSpawnRawPositions = new Dictionary<GameObject, Vector3>();

    private float minDistanceFromTarget = 50f;
    private float maxDistanceFromTarget = 150f;
    private LookDirection lookDirection = LookDirection.TOWARDS_TARGET;

    private int AttemptCountMax { get; set; } = 3;
    private int AttemptCount { get; set; } = 0;
    private int TotalAttemptMax { get; set; } = 9;
    private int TotalAttemptCount { get; set; } = 0;

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, string objectKey, string orientationTargetKey, LookDirection lookDirection) : base(encounterRules) {
      this.configuredObjectKeys = new List<string> { objectKey };
      this.defaultOrientationTargetKey = orientationTargetKey;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, string objectKey, string orientationTargetKey, LookDirection lookDirection, float minDistance, float maxDistance) : base(encounterRules) {
      this.configuredObjectKeys = new List<string> { objectKey };
      this.defaultOrientationTargetKey = orientationTargetKey;
      this.minDistanceFromTarget = minDistance;
      this.maxDistanceFromTarget = maxDistance;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, List<string> objectKeys, string orientationTargetKey, LookDirection lookDirection) : base(encounterRules) {
      this.configuredObjectKeys = new List<string>(objectKeys);
      this.defaultOrientationTargetKey = orientationTargetKey;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, List<string> objectKeys, string orientationTargetKey, LookDirection lookDirection, float minDistance, float maxDistance) : base(encounterRules) {
      this.configuredObjectKeys = new List<string>(objectKeys);
      this.defaultOrientationTargetKey = orientationTargetKey;
      this.minDistanceFromTarget = minDistance;
      this.maxDistanceFromTarget = maxDistance;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, LogicState state, LookDirection lookDirection) : base(encounterRules) {
      this.state = state;
      this.lookDirection = lookDirection;
    }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, LogicState state, LookDirection lookDirection, float minDistance, float maxDistance)
      : this(encounterRules, state, lookDirection, minDistance, maxDistance, false) { }

    public SpawnObjectsAroundTarget(EncounterRules encounterRules, LogicState state, LookDirection lookDirection, float minDistance, float maxDistance, bool onlyRepositionIfInvalid) : base(encounterRules) {
      this.state = state;
      this.minDistanceFromTarget = minDistance;
      this.maxDistanceFromTarget = maxDistance;
      this.lookDirection = lookDirection;
      this.onlyRepositionIfInvalid = onlyRepositionIfInvalid;
    }

    public override void Run(RunPayload payload) {
      if (!GetObjectReferences()) return;

      // GUARD: Every object should have a key and orientation target. Otherwise something went wrong.
      if (objectKeys.Count != objectGos.Count) {
        Main.Logger.LogError($"[SpawnObjectsAroundTarget] Objects do not have a matching set of 'object keys' for this spawner to work correctly.");
        return;
      }

      if (orientationTargetKeys.Count != objectGos.Count) {
        Main.Logger.LogError($"[SpawnObjectsAroundTarget] Objects do not have a matching set of 'orientation target keys' for this spawner to work correctly.");
        return;
      }

      RefreshCachedSpawnOccupancy();

      for (int i = 0; i < objectGos.Count; i++) {
        GameObject objectGo = objectGos[i];
        if (objectGo == null) {
          Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] Found a null object. Skipping.'");
          continue;
        }
        Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] Attempting for '{objectGo.name}'");


        string objectKey = (objectKeys.Count > i) ? objectKeys[i] : "INVALID";
        Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] With key '{objectKey}'");
        string orientationTargetKey = orientationTargetKeys[i];
        Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] And orientation target '{orientationTargetKey}'");

        if (!orientationTargets.ContainsKey(orientationTargetKey) || orientationTargets[orientationTargetKey] == null) {
          Main.Logger.LogError($"[SpawnObjectsAroundTarget] Orientation target with key '{orientationTargetKey}' does not exist. This is required for this spawner to work correctly.");
          continue;
        }

        GameObject orientationTarget = orientationTargets[orientationTargetKey];
        Main.Logger.LogDebug($"[SpawnObjectsAroundTarget] Using orientation target key '{orientationTargetKey}' and Go name '{orientationTarget.transform.name}'");

        SpawnObjectAroundTarget(objectGo, orientationTarget);
      }
    }

    private void SpawnObjectAroundTarget(GameObject objectGo, GameObject orientationTarget) {
      SaveSpawnPosition(objectGo);

      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      Vector3 validOrientationTargetPosition = GetValidOrientationTargetPosition(orientationTarget);

      bool isPlayerObject = (objectGo.transform.parent != null && objectGo.transform.parent.gameObject == EncounterRules.SpawnerPlayerLanceGo)
        || objectGo.GetComponentInParent<PlayerLanceSpawnerGameLogic>() != null;
      Vector3 playerConnectivityTarget = (!isPlayerObject && EncounterRules.SpawnerPlayerLanceGo != null)
        ? ResolveVerifiedOrientationTargetPosition(EncounterRules.SpawnerPlayerLanceGo, "PlayerLanceConnectivity")
        : Vector3.zero;
      Vector3 candidatePathfindTarget = isPlayerObject ? validOrientationTargetPosition : playerConnectivityTarget;
      Quaternion originalRotation = objectGo.transform.rotation;

      if (onlyRepositionIfInvalid) {
        Vector3 currentPos = objectGo.transform.position.GetClosestHexLerpedPointOnGrid();
        bool withinFormationRange = IsWithinFormationRange(objectGo, currentPos, validOrientationTargetPosition);
        bool validAtCurrentPos = withinFormationRange
          && !IsHexOccupiedByAnotherSpawn(currentPos, objectGo)
          && encounterLayerData.IsInEncounterBounds(currentPos)
          && IsObjectPositionReachable(combatState, objectGo, currentPos, validOrientationTargetPosition, playerConnectivityTarget, skipOrientationCheck: false, skipPlayerCheck: false);
        if (validAtCurrentPos) {
          objectGo.transform.position = currentPos;
          UpdateCachedSpawnOccupancy(objectGo, currentPos);
          Main.LogDebug($"[SpawnObjectsAroundTarget] '{objectGo.name}' is already at a valid reachable position '{currentPos}'. Keeping existing placement.");
          return;
        }
      }

      float currentMinDistance = minDistanceFromTarget;
      float currentMaxDistance = maxDistanceFromTarget;

      while (TotalAttemptCount < TotalAttemptMax) {
        Vector3 newSpawnPosition = GetRandomPositionFromTarget(validOrientationTargetPosition, currentMinDistance, currentMaxDistance);
        bool verifiedCandidateTarget = false;
        bool foundSpawn = false;

        if (candidatePathfindTarget != Vector3.zero
            && newSpawnPosition.DistanceFlat(candidatePathfindTarget) <= 25f
            && !IsHexOccupiedByAnotherSpawn(newSpawnPosition, objectGo)
            && PathFinderManager.Instance.IsAdjacentReachableSpawn(combatState, objectGo, newSpawnPosition, candidatePathfindTarget, UnitType.Mech)) {
          foundSpawn = true;
          verifiedCandidateTarget = true;
        } else {
          foundSpawn = TryGetClosestValidPathFindingHex(objectGo, newSpawnPosition, $"NewRandomSpawnPositionFromOrientationTarget.{orientationTarget.name}", candidatePathfindTarget, out newSpawnPosition, out verifiedCandidateTarget, 2);
        }

        if (foundSpawn && encounterLayerData.IsInEncounterBounds(newSpawnPosition) && !IsHexOccupiedByAnotherSpawn(newSpawnPosition, objectGo)) {
          objectGo.transform.position = newSpawnPosition;

          if (onlyRepositionIfInvalid) {
            objectGo.transform.rotation = originalRotation;
          } else if (lookDirection == LookDirection.TOWARDS_TARGET) {
            RotateToTarget(objectGo, orientationTarget);
          } else {
            RotateAwayFromTarget(objectGo, orientationTarget);
          }

          bool skipOrientation = verifiedCandidateTarget && isPlayerObject;
          bool skipPlayer = verifiedCandidateTarget && !isPlayerObject && playerConnectivityTarget != Vector3.zero;
          if (IsObjectPositionReachable(combatState, objectGo, newSpawnPosition, validOrientationTargetPosition, playerConnectivityTarget, skipOrientation, skipPlayer)) {
            UpdateCachedSpawnOccupancy(objectGo, newSpawnPosition);
            Main.Logger.Log("[SpawnObjectsAroundTarget] Object spawn complete");
            ResetAttempts();
            return;
          }
        } else {
          Main.LogDebugWarning("[SpawnObjectsAroundTarget] Selected object spawn point is invalid or outside of the boundary. Select a new object spawn point.");
        }

        CheckAttempts(ref currentMinDistance, ref currentMaxDistance);
      }

      // Bounded nearest-first ring search around the verified orientation target before falling back to the original position
      if (combatState != null) {
        List<Vector3> ringHexes = combatState.HexGrid.GetGridPointsAroundPointWithinRadius(validOrientationTargetPosition, 10);
        ringHexes.Sort((a, b) => (a - validOrientationTargetPosition).sqrMagnitude.CompareTo((b - validOrientationTargetPosition).sqrMagnitude));
        int terrainBudget = 160;
        int pathfindBudget = 40;
        foreach (Vector3 rawHex in ringHexes) {
          if (terrainBudget <= 0 || pathfindBudget <= 0) break;
          Vector3 hex = rawHex.GetClosestHexLerpedPointOnGrid();
          if (!encounterLayerData.IsInEncounterBounds(hex)) continue;

          terrainBudget--;
          if (!PathFinderManager.Instance.IsCellValidDrySpawn(combatState, hex)) continue;
          if (IsHexOccupiedByAnotherSpawn(hex, objectGo)) continue;

          pathfindBudget--;
          if (!IsObjectPositionReachable(combatState, objectGo, hex, validOrientationTargetPosition, playerConnectivityTarget, skipOrientationCheck: false, skipPlayerCheck: false)) continue;

          objectGo.transform.position = hex;
          UpdateCachedSpawnOccupancy(objectGo, hex);
          if (onlyRepositionIfInvalid) {
            objectGo.transform.rotation = originalRotation;
          } else if (lookDirection == LookDirection.TOWARDS_TARGET) {
            RotateToTarget(objectGo, orientationTarget);
          } else {
            RotateAwayFromTarget(objectGo, orientationTarget);
          }
          Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] Placed '{objectGo.name}' at nearest reachable fallback '{hex}'.");
          ResetAttempts();
          return;
        }
      }

      Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] Could not find a valid spawn for '{objectGo.name}' after '{TotalAttemptCount}' attempts. Restoring its original position.");
      RestoreSpawnPosition(objectGo);
      objectGo.transform.rotation = originalRotation;
      UpdateCachedSpawnOccupancy(objectGo, objectGo.transform.position.GetClosestHexLerpedPointOnGrid());
      ResetAttempts();
    }

    private void RefreshCachedSpawnOccupancy() {
      cachedSpawnSnappedPositions.Clear();
      cachedSpawnRawPositions.Clear();
      EncounterLayerData encounterLayerData = MissionControl.Instance.EncounterLayerData;
      if (encounterLayerData == null) return;

      UnitSpawnPointGameLogic[] unitSpawns = encounterLayerData.GetComponentsInChildren<UnitSpawnPointGameLogic>();
      foreach (UnitSpawnPointGameLogic unitSpawn in unitSpawns) {
        if (unitSpawn == null) continue;
        GameObject go = unitSpawn.gameObject;
        cachedSpawnRawPositions[go] = go.transform.position;
        cachedSpawnSnappedPositions[go] = go.transform.position.GetClosestHexLerpedPointOnGrid();
      }
    }

    private void UpdateCachedSpawnOccupancy(GameObject go, Vector3 snappedPos) {
      if (go == null) return;
      cachedSpawnRawPositions[go] = go.transform.position;
      cachedSpawnSnappedPositions[go] = snappedPos;
    }

    private bool IsWithinFormationRange(GameObject objectGo, Vector3 candidatePos, Vector3 fallbackAnchorPos) {
      if (objectGo.transform.parent != null) {
        List<GameObject> siblings = objectGo.transform.parent.gameObject.FindAllContains("SpawnPoint");
        float nearestOriginalDist = float.MaxValue;
        bool foundOriginalSibling = false;
        foreach (GameObject sibling in siblings) {
          if (sibling == null || sibling == objectGo || objectGos.Contains(sibling)) continue;
          float dist = candidatePos.DistanceFlat(sibling.transform.position);
          if (dist < nearestOriginalDist) {
            nearestOriginalDist = dist;
            foundOriginalSibling = true;
          }
        }
        if (foundOriginalSibling) {
          return nearestOriginalDist <= maxDistanceFromTarget;
        }
      }
      return candidatePos.DistanceFlat(fallbackAnchorPos) <= maxDistanceFromTarget;
    }

    private bool IsHexOccupiedByAnotherSpawn(Vector3 snappedHex, GameObject ignoreGo) {
      foreach (KeyValuePair<GameObject, Vector3> entry in cachedSpawnSnappedPositions) {
        GameObject otherGo = entry.Key;
        if (otherGo == null || otherGo == ignoreGo) continue;
        if (snappedHex.DistanceFlat(entry.Value) < 13f) return true;
        if (cachedSpawnRawPositions.TryGetValue(otherGo, out Vector3 rawPos) && Vector3.Distance(rawPos, snappedHex) < 13f) return true;
      }
      return false;
    }

    private bool IsObjectPositionReachable(
      CombatGameState combatState,
      GameObject objectGo,
      Vector3 candidatePos,
      Vector3 orientationTargetPos,
      Vector3 playerConnectivityTarget,
      bool skipOrientationCheck,
      bool skipPlayerCheck
    ) {
      if (!skipOrientationCheck) {
        bool reachesOrientation = (candidatePos.DistanceFlat(orientationTargetPos) <= 25f)
          ? PathFinderManager.Instance.IsAdjacentReachableSpawn(combatState, objectGo, candidatePos, orientationTargetPos, UnitType.Mech)
          : PathFinderManager.Instance.IsSpawnValid(objectGo, candidatePos, orientationTargetPos, UnitType.Mech, objectGo.name);
        if (!reachesOrientation) return false;
      }

      if (!skipPlayerCheck && playerConnectivityTarget != Vector3.zero && playerConnectivityTarget.DistanceFlat(orientationTargetPos) > 1f) {
        bool reachesPlayer = (candidatePos.DistanceFlat(playerConnectivityTarget) <= 25f)
          ? PathFinderManager.Instance.IsAdjacentReachableSpawn(combatState, objectGo, candidatePos, playerConnectivityTarget, UnitType.Mech)
          : PathFinderManager.Instance.IsSpawnValid(objectGo, candidatePos, playerConnectivityTarget, UnitType.Mech, objectGo.name);
        if (!reachesPlayer) return false;
      }

      return true;
    }

    private Vector3 GetValidOrientationTargetPosition(GameObject orientationTarget) {
      if (validOrientationTargetPositions.ContainsKey(orientationTarget)) {
        Vector3 validPosition = validOrientationTargetPositions[orientationTarget];
        Main.LogDebug($"[SpawnObjectsAroundTarget] Reusing cached orientation target of '{orientationTarget.name}' at '{validPosition}'.");
        return validPosition;
      }

      Main.LogDebug($"[SpawnObjectsAroundTarget] Orientation target of '{orientationTarget.name}' at '{orientationTarget.transform.position}'. Resolving verified target hex.");
      Vector3 validOrientationTargetPosition = ResolveVerifiedOrientationTargetPosition(orientationTarget, $"OrientationTarget.{orientationTarget.name}");
      validOrientationTargetPositions[orientationTarget] = validOrientationTargetPosition;

      return validOrientationTargetPosition;
    }

    private void CheckAttempts(ref float currentMinDistance, ref float currentMaxDistance) {
      AttemptCount++;
      TotalAttemptCount++;

      if (AttemptCount >= AttemptCountMax && TotalAttemptCount < TotalAttemptMax) {
        AttemptCount = 0;
        Main.LogDebug($"[SpawnObjectsAroundTarget] Cannot find a suitable object spawn within the boundaries of {currentMinDistance} and {currentMaxDistance}. Widening search");
        currentMinDistance -= 10;
        if (currentMinDistance <= 10) currentMinDistance = 10;
        currentMaxDistance += 25;
      }
    }

    private void ResetAttempts() {
      AttemptCount = 0;
      TotalAttemptCount = 0;
    }

    protected override bool GetObjectReferences() {
      objectKeys.Clear();
      orientationTargetKeys.Clear();
      keyLookup.Clear();
      objectGos.Clear();
      orientationTargets.Clear();

      if (state != null) {
        List<string[]> extraLanceKeys = (List<string[]>)state.GetObject("ExtraLanceSpawnKeys");
        if (extraLanceKeys == null) {
          Main.Logger.LogWarning("[SpawnObjectsAroundTarget] ExtraLanceSpawnKeys was not found in state.");
          return false;
        }
        for (int i = 0; i < extraLanceKeys.Count; i++) {
          string[] keys = extraLanceKeys[i];

          if (keys != null && keys.Length > 0) {
            if (keys.Length == 2) {
              string objectKey = keys[0];
              string orientationObjectKey = keys[1];

              // Check if the key object and orientation object really exist
              if (objectKey == null || objectKey == "") {
                Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] GameObject has a null or empty object key. This is unexpected.");
                continue;
              }

              GameObject objectKeyGO = null;
              if (!this.EncounterRules.ObjectLookup.TryGetValue(objectKey, out objectKeyGO) || objectKeyGO == null) {
                objectKeyGO = GameObject.Find(objectKey.Substring(objectKey.IndexOf(".") + 1));
              }
              if (objectKeyGO == null) {
                Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] GameObject with key '{objectKey}' was not found. This is unexpected.");
                continue;
              }

              if (orientationObjectKey == null || orientationObjectKey == "") {
                Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] GameObject has a null or empty orientation object key. This is unexpected.");
                continue;
              }

              GameObject orientationObjectKeyGO = null;
              if (!this.EncounterRules.ObjectLookup.TryGetValue(orientationObjectKey, out orientationObjectKeyGO) || orientationObjectKeyGO == null) {
                orientationObjectKeyGO = GameObject.Find(orientationObjectKey.Substring(orientationObjectKey.IndexOf(".") + 1));
              }
              if (orientationObjectKeyGO == null) {
                Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] GameObject for orientation target with key '{orientationObjectKey}' was not found. This is unexpected.");
                continue;
              }

              objectKeys.Add(objectKey);
              orientationTargetKeys.Add(orientationObjectKey);
              keyLookup[objectKey] = orientationObjectKey;
            } else {
              Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] ExtraLanceSpawnKeys keyset does not provide exactly a key and orientation key.");
            }
          } else {
            Main.Logger.LogWarning($"[SpawnObjectsAroundTarget] ExtraLanceSpawnKeys provides a first key set but the keyset is null or empty");
          }
        }
      } else {
        for (int i = 0; i < configuredObjectKeys.Count; i++) {
          string objectKey = configuredObjectKeys[i];

          objectKeys.Add(objectKey);
          orientationTargetKeys.Add(defaultOrientationTargetKey);
          keyLookup[objectKey] = defaultOrientationTargetKey;
        }
      }

      for (int i = 0; i < objectKeys.Count; i++) {
        GameObject objectGoShell;
        this.EncounterRules.ObjectLookup.TryGetValue(objectKeys[i], out objectGoShell);
        objectGos.Add(objectGoShell);
      }

      for (int i = 0; i < orientationTargetKeys.Count; i++) {
        GameObject orientationTargetGoShell;
        this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKeys[i], out orientationTargetGoShell);
        orientationTargets[orientationTargetKeys[i]] = orientationTargetGoShell;
      }

      if (defaultOrientationTargetKey != "") {
        GameObject defaultOrientationTargetGoShell;
        this.EncounterRules.ObjectLookup.TryGetValue(defaultOrientationTargetKey, out defaultOrientationTargetGoShell);
        orientationTargets[defaultOrientationTargetKey] = defaultOrientationTargetGoShell;
      }

      return true;
    }
  }
}
