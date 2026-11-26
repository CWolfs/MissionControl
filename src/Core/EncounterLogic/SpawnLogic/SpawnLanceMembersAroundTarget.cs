using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Designed;

using MissionControl.Rules;
using MissionControl.Utils;

namespace MissionControl.Logic {
  public class SpawnLanceMembersAroundTarget : SpawnLanceLogic {
    private string lanceKey = "";
    private string orientationTargetKey = "";
    private string lookTargetKey = "";

    private GameObject lance;
    private GameObject orientationTarget;
    private GameObject lookTarget;
    private LookDirection lookDirection;
    private float mustBeBeyondDistance = 50f;
    private float mustBeWithinDistance = 100f;
    private List<Vector3> invalidSpawnLocations = new List<Vector3>();

    private int AttemptCountMax { get; set; } = 5;
    private int AttemptCount { get; set; } = 0;
    private int TotalAttemptMax { get; set; } = 20;
    private int TotalAttemptCount { get; set; } = 0;

    private RunPayload payload;
    private Vector3 playableAreaReference = Vector3.zero;

    public SpawnLanceMembersAroundTarget(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, LookDirection lookDirection) :
      this(encounterRules, lanceKey, orientationTargetKey, lookDirection, 10, 10) { } // TODO: Replace the hard coded values with a setting.json setting

    public SpawnLanceMembersAroundTarget(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, LookDirection lookDirection, float mustBeBeyondDistance, float mustBeWithinDistance) :
      this(encounterRules, lanceKey, orientationTargetKey, orientationTargetKey, lookDirection, mustBeBeyondDistance, mustBeWithinDistance) { }

    public SpawnLanceMembersAroundTarget(EncounterRules encounterRules, string lanceKey, string orientationTargetKey, string lookTargetKey, LookDirection lookDirection, float mustBeBeyondDistance, float mustBeWithinDistance) : base(encounterRules) {
      this.lanceKey = lanceKey;
      this.orientationTargetKey = orientationTargetKey;
      this.lookTargetKey = lookTargetKey;
      this.lookDirection = lookDirection;
      this.mustBeBeyondDistance = mustBeBeyondDistance;
      this.mustBeWithinDistance = mustBeWithinDistance;
    }

    public override string TargetLanceKey => lanceKey;

    public override void Run(RunPayload payload) {
      if (!GetObjectReferences()) return;

      this.payload = payload;
      SaveSpawnPositions(lance);
      Main.Logger.Log($"[SpawnLanceMembersAroundTarget] Attempting for '{lance.name}'");
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      playableAreaReference = Vector3.zero;
      if (IsLancePlayerLance(lanceKey)) {
        if (lookTarget != null && lookTarget != orientationTarget && lookTarget != lance) {
          playableAreaReference = ResolveVerifiedOrientationTargetPosition(lookTarget, $"PlayableRef.{lookTarget.name}");
        } else if (HasSavedOriginalPositions() && !IsUnplacedGeneratedLance(lance)) {
          playableAreaReference = ResolveVerifiedOrientationTargetPosition(lance, $"PlayableRef.{lance.name}");
        }
      }

      Vector3 validOrientationTargetPosition = ResolveVerifiedOrientationTargetPosition(orientationTarget, $"OrientationTarget.{orientationTarget.name}");
      lance.transform.position = validOrientationTargetPosition;

      List<GameObject> spawnPoints = lance.FindAllContains("SpawnPoint");
      foreach (GameObject spawnPoint in spawnPoints) {
        bool success = SpawnLanceMember(spawnPoint, validOrientationTargetPosition, lookTarget, lookDirection);
        if (!success) break;
      }

      invalidSpawnLocations.Clear();
    }

    protected override bool GetObjectReferences() {
      this.EncounterRules.ObjectLookup.TryGetValue(lanceKey, out lance);
      this.EncounterRules.ObjectLookup.TryGetValue(orientationTargetKey, out orientationTarget);
      this.EncounterRules.ObjectLookup.TryGetValue(lookTargetKey, out lookTarget);

      if (lance == null) {
        Main.Logger.LogWarning($"[SpawnLanceMembersAroundTarget] Object reference for target '{lanceKey}' is null. This will be handled gracefully.");
        return false;
      }

      if (orientationTarget == null) {
        Main.Logger.LogWarning($"[SpawnLanceMembersAroundTarget] Object reference for orientation target '{orientationTargetKey}' is null. This will be handled gracefully.");
        return false;
      }

      if (lookTarget == null) {
        Main.Logger.LogWarning($"[SpawnLanceMembersAroundTarget] Object reference for look target '{lookTarget}' is null. This will be handled gracefully.");
        return false;
      }

      return true;
    }

    public bool SpawnLanceMember(GameObject spawnPoint, Vector3 orientationTargetPosition, GameObject lookTarget, LookDirection lookDirection) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      MissionControl encounterManager = MissionControl.Instance;

      if (TotalAttemptCount >= TotalAttemptMax) {
        HandleFallback(this.payload, this.lanceKey, this.orientationTargetKey);
        return false;
      }

      Vector3 newSpawnPosition = GetRandomPositionFromTarget(orientationTargetPosition, mustBeBeyondDistance, mustBeWithinDistance);
      Vector3 pathfindTarget = IsLancePlayerLance(lanceKey) ? orientationTargetPosition : Vector3.zero;
      bool foundSpawn = TryGetClosestValidPathFindingHex(spawnPoint, newSpawnPosition, $"NewRandomSpawnPositionFromOrientationTarget.{orientationTarget.name}", pathfindTarget, out newSpawnPosition, 2);

      if (foundSpawn && encounterManager.EncounterLayerData.IsInEncounterBounds(newSpawnPosition)) {
        if (!IsWithinDistanceOfInvalidPosition(newSpawnPosition) && !newSpawnPosition.IsTooCloseToAnotherSpawn(spawnPoint)) {
          spawnPoint.transform.position = newSpawnPosition;

          if (lookDirection == LookDirection.TOWARDS_TARGET) {
            RotateToTarget(spawnPoint, lookTarget);
          } else {
            RotateAwayFromTarget(spawnPoint, lookTarget);
          }

          bool validForPlayableArea = (playableAreaReference == Vector3.zero)
            || (playableAreaReference.DistanceFlat(orientationTargetPosition) <= 1f)
            || IsSpawnValid(spawnPoint, playableAreaReference);

          if (!IsSpawnValid(spawnPoint, orientationTargetPosition) || !validForPlayableArea) {
            CheckAttempts();
            return SpawnLanceMember(spawnPoint, orientationTargetPosition, lookTarget, lookDirection);
          } else {
            invalidSpawnLocations.Add(newSpawnPosition);
            AttemptCount = 0;
            Main.Logger.Log("[SpawnLanceMembersAroundTarget] Lance member spawn complete");
          }
        } else {
          Main.LogDebugWarning("[SpawnLanceMembersAroundTarget] Cannot spawn a lance member on an invalid spawn. Finding new spawn point.");
          CheckAttempts();
          return SpawnLanceMember(spawnPoint, orientationTargetPosition, lookTarget, lookDirection);
        }
      } else {
        Main.LogDebugWarning("[SpawnLanceMembersAroundTarget] Selected lance spawn point is invalid or outside of the boundary. Select a new lance spawn point.");
        CheckAttempts();
        return SpawnLanceMember(spawnPoint, orientationTargetPosition, lookTarget, lookDirection);
      }

      return true;
    }

    private bool IsWithinDistanceOfInvalidPosition(Vector3 newSpawn) {
      foreach (Vector3 invalidSpawn in invalidSpawnLocations) {
        Vector3 vectorToInvalidSpawn = newSpawn - invalidSpawn;
        vectorToInvalidSpawn.y = 0;
        float distanceToInvalidSpawn = vectorToInvalidSpawn.magnitude;
        if (distanceToInvalidSpawn < minDistanceToSpawnFromInvalidSpawn) return true;
      }
      return false;
    }

    private void CheckAttempts() {
      AttemptCount++;
      TotalAttemptCount++;

      if (AttemptCount > AttemptCountMax) {
        AttemptCount = 0;
        Main.Logger.Log($"[SpawnLanceMembersAroundTarget] Cannot find a suitable lance member spawn within the boundaries of {mustBeBeyondDistance} and {mustBeWithinDistance}. Widening search");
        mustBeBeyondDistance -= 10;
        if (mustBeBeyondDistance <= 10) mustBeBeyondDistance = 10;
        mustBeWithinDistance += 25;
        if (mustBeWithinDistance > 700) mustBeWithinDistance = 700;
      }
    }
  }
}