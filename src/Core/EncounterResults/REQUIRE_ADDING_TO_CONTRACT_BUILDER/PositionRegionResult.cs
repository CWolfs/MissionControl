using UnityEngine;

using BattleTech;

using System.Collections.Generic;

/**
	This result will reposition a region within a min and max threshold. 
	It will also recreate the Mesh to match the terrain for triggering the region correctly
*/
namespace MissionControl.Result {
  public class PositionRegionResult : EncounterResult {
    // TODO: Replace this ideally with GUID and not name
    public string RegionName { get; set; } = "";

    private const int MAX_ATTEMPTS = 100;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug("[PositionRegion] Positioning Region...");
      GameObject regionGo = GameObject.Find(RegionName);

      if (regionGo == null) {
        Main.Logger.LogError($"[PositionRegion] Cannot find region GameObject with name '{RegionName}'. Aborting region positioning.");
        return;
      }

      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      if (combatState == null || combatState.LocalPlayerTeam == null) {
        Main.Logger.LogError("[PositionRegion] Combat state not ready. Aborting region positioning.");
        return;
      }
      Team playerTeam = combatState.LocalPlayerTeam;

      // Get living player units only (with valid GameRep)
      List<AbstractActor> livingPlayerActors = combatState.AllActors.FindAll(x => x.TeamId == playerTeam.GUID && !x.IsDead && x.GameRep != null);

      if (livingPlayerActors.Count == 0) {
        Main.Logger.LogError($"[PositionRegion] No living actors found for player team '{playerTeam.GUID}'. Aborting region positioning.");
        return;
      }

      Vector3 centerOfTeamMass = GetCenterOfTeamMass(playerTeam, true);
      Vector3 awayDirection = GetAwayFromEnemiesDirection(playerTeam);
      Vector3 possiblePosition = Vector3.zero;
      int attempts = 0;
      bool useDirectionalBias = awayDirection != Vector3.zero;

      // Retry logic with fallback strategies
      while (possiblePosition == Vector3.zero || !IsPositionAccessibleByPlayers(regionGo, possiblePosition, livingPlayerActors, attempts)) {
        Main.LogDebug($"[PositionRegion] {(possiblePosition == Vector3.zero ? "Finding possible position..." : "Trying again to find a possible position...")} (Attempt {attempts + 1}/{MAX_ATTEMPTS})");

        // Attempt 1-30: Use directional bias away from enemies
        if (attempts < 30 && useDirectionalBias) {
          possiblePosition = SceneUtils.GetRandomPositionInDirection(
            centerOfTeamMass,
            awayDirection,
            Main.Settings.DynamicWithdraw.MinDistanceForZone,
            Main.Settings.DynamicWithdraw.MaxDistanceForZone,
            90f // 90 degree cone
          );
        }
        // Attempt 31-60: Fallback to non-directional (wider search)
        else if (attempts < 60) {
          if (attempts == 30) Main.Logger.LogWarning($"[PositionRegion] Falling back to non-directional search for region '{RegionName}'.");
          possiblePosition = SceneUtils.GetRandomPositionFromTarget(
            centerOfTeamMass,
            Main.Settings.DynamicWithdraw.MinDistanceForZone,
            Main.Settings.DynamicWithdraw.MaxDistanceForZone
          );
        }
        // Attempt 61-100: Increase search radius
        else {
          if (attempts == 60) Main.Logger.LogWarning($"[PositionRegion] Increasing search radius for region '{RegionName}'.");
          possiblePosition = SceneUtils.GetRandomPositionFromTarget(
            centerOfTeamMass,
            Main.Settings.DynamicWithdraw.MinDistanceForZone * 0.5f,  // Reduce min distance
            Main.Settings.DynamicWithdraw.MaxDistanceForZone * 1.5f   // Increase max distance
          );
        }

        attempts++;
        if (attempts >= MAX_ATTEMPTS) {
          Main.Logger.LogError($"[PositionRegion] Failed to find valid position for region '{RegionName}' after {MAX_ATTEMPTS} attempts. Aborting region positioning.");
          return;
        }
      }

      regionGo.transform.position = possiblePosition;

      // Debug
      // GameObjextExtensions.CreateDebugPoint("DEBUGCenterofTeamMassGizmo", centerOfTeamMass, Color.red);
      // GameObjextExtensions.CreateDebugPoint("DEBUGDynamicWithdrawCenter", regionGo.transform.position, Color.blue);

      RegionGameLogic regionGameLogic = regionGo.GetComponent<RegionGameLogic>();

      if (regionGameLogic == null) {
        Main.Logger.LogError($"[PositionRegion] Region GameObject '{RegionName}' does not have a RegionGameLogic component. Cannot regenerate.");
        return;
      }

      regionGameLogic.Regenerate();
    }

    private Vector3 GetCenterOfTeamMass(Team team, bool avoidEnemies) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;
      List<AbstractActor> teamActors = combatState.AllActors.FindAll((AbstractActor x) => x.TeamId == team.GUID);
      List<AbstractActor> avoidTeamActors = null;

      if (avoidEnemies) avoidTeamActors = combatState.AllEnemies;

      return SceneUtils.CalculateCentroidOfActors(teamActors, avoidTeamActors);
    }

    private Vector3 GetAwayFromEnemiesDirection(Team playerTeam) {
      CombatGameState combatState = UnityGameInstance.BattleTechGame.Combat;

      // Get living player and enemy actors (with valid GameRep)
      List<AbstractActor> livingPlayerActors = combatState.AllActors.FindAll(x => x.TeamId == playerTeam.GUID && !x.IsDead && x.GameRep != null);
      List<AbstractActor> livingEnemyActors = combatState.AllEnemies.FindAll(x => !x.IsDead && x.GameRep != null);

      if (livingPlayerActors.Count == 0 || livingEnemyActors.Count == 0) {
        return Vector3.zero;
      }

      // Calculate player centroid first
      Vector3 playerCentroid = Vector3.zero;
      foreach (AbstractActor actor in livingPlayerActors) {
        Vector3 pos = actor.GameRep.transform.position;
        pos.y = 0;
        playerCentroid += pos;
      }
      playerCentroid /= livingPlayerActors.Count;

      // Filter enemies by distance to player centroid - only consider nearby threats
      // Use MaxDistanceForZone × 2.5 as threat range (scales with settings)
      float threatRange = Main.Settings.DynamicWithdraw.MaxDistanceForZone * 2.5f;
      List<AbstractActor> nearbyEnemies = livingEnemyActors.FindAll(enemy => {
        Vector3 enemyPos = enemy.GameRep.transform.position;
        enemyPos.y = 0;
        float distance = (enemyPos - playerCentroid).magnitude;
        return distance <= threatRange;
      });

      // Fallback: if no enemies in threat range, use all enemies (edge case for very spread out battles)
      List<AbstractActor> relevantEnemies = nearbyEnemies.Count > 0 ? nearbyEnemies : livingEnemyActors;

      if (nearbyEnemies.Count < livingEnemyActors.Count) {
        Main.LogDebug($"[GetAwayFromEnemiesDirection] Filtering enemies: {nearbyEnemies.Count}/{livingEnemyActors.Count} within threat range ({threatRange:F0} units)");
      }

      // Calculate enemy centroid from relevant enemies only
      Vector3 enemyCentroid = Vector3.zero;
      foreach (AbstractActor actor in relevantEnemies) {
        Vector3 pos = actor.GameRep.transform.position;
        pos.y = 0;
        enemyCentroid += pos;
      }
      enemyCentroid /= relevantEnemies.Count;

      // Direction from enemies toward players
      Vector3 direction = playerCentroid - enemyCentroid;

      if (direction.magnitude < 10f) {
        return Vector3.zero; // Too close to determine meaningful direction
      }

      return direction.normalized;
    }

    private bool IsPositionAccessibleByPlayers(GameObject regionGo, Vector3 position, List<AbstractActor> livingPlayerActors, int attemptNumber) {
      // Test pathfinding from each living player unit to the extraction zone
      // This validates both: (1) the zone is on valid terrain (not water, etc.),
      // and (2) units can actually reach it
      int totalUnits = livingPlayerActors.Count;
      int unitsWithValidPath = 0;

      // Calculate required percentage and unit count for early exit
      float requiredPercentage;
      if (attemptNumber < 30) {
        requiredPercentage = 0.75f; // 75% for first 30 attempts
      } else if (attemptNumber < 60) {
        requiredPercentage = 0.5f; // 50% for attempts 31-60
      } else {
        requiredPercentage = 0.33f; // 33% for attempts 61-100 (at least 1/3 can reach)
      }
      int requiredUnits = Mathf.CeilToInt(totalUnits * requiredPercentage);

      foreach (AbstractActor actor in livingPlayerActors) {
        // Test if this unit can path to the extraction zone
        // IsSpawnValid checks both terrain validity and pathability
        bool canPath = PathFinderManager.Instance.IsSpawnValid(
          regionGo,
          position,
          actor.GameRep.transform.position,
          UnitType.Mech,
          $"PositionRegionResult.{RegionName}"
        );

        if (canPath) {
          unitsWithValidPath++;
          // Early exit optimization: stop testing once we have enough valid paths
          if (unitsWithValidPath >= requiredUnits) {
            Main.LogDebug($"[PositionRegion] Early exit: {unitsWithValidPath}/{totalUnits} units can reach position (need {requiredPercentage * 100:F0}%)");
            break;
          }
        }
      }

      // If NO units can reach the position, it's definitely invalid
      // (likely bad terrain like deep water, lava, etc.)
      if (unitsWithValidPath == 0) {
        Main.LogDebug("[PositionRegion] Position rejected: NO units can reach it (likely invalid terrain)");
        return false;
      }

      // Check if we met the required percentage (already calculated above)
      float actualPercentage = (float)unitsWithValidPath / totalUnits;
      bool isAccessible = actualPercentage >= requiredPercentage;

      if (!isAccessible) {
        Main.LogDebug($"[PositionRegion] Only {unitsWithValidPath}/{totalUnits} units can path to position (need {requiredPercentage * 100:F0}%)");
      } else {
        Main.LogDebug($"[PositionRegion] Position is accessible: {unitsWithValidPath}/{totalUnits} units can reach it ({actualPercentage * 100:F0}%)");
      }

      return isAccessible;
    }
  }
}
