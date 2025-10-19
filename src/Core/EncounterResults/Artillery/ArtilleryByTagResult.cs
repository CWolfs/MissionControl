using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;

using HBS.Collections;

using MissionControl.Utils;

namespace MissionControl.Result {
  public enum ArtilleryTargetPriority {
    None,
    Units,
    Buildings
  }

  public class ArtilleryByTagResult : EncounterResult {
    private const float MISS_DISTANCE = 72.0f;
    private const float MISS_DISTANCE_VARIANCE = 24.0f;

    public string Name { get; set; }
    public string Description { get; set; }
    public string[] TargetTags { get; set; }
    public bool ShotsFireAtAllTargets { get; set; } = true;
    public float ChanceToHit { get; set; } = 1.0f;
    public int Damage { get; set; }
    public int HeatDamage { get; set; }
    public int StabilityDamage { get; set; }
    public ArtilleryVFXType ArtilleryVFXType { get; set; } = ArtilleryVFXType.ArtilleryShellBarrage;
    public ArtilleryTargetPriority TargetPriority { get; set; } = ArtilleryTargetPriority.None;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[ArtilleryByTagResult] Triggering artillery strike '{Name}' - {Description}");

      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;

      // Validate ChanceToHit
      if (ChanceToHit < 0.0f || ChanceToHit > 1.0f) {
        Main.Logger.LogError($"[ArtilleryByTagResult] ChanceToHit must be between 0.0 and 1.0. Got: {ChanceToHit}. Clamping to valid range.");
        ChanceToHit = Mathf.Clamp01(ChanceToHit);
      }

      // Validate TargetTags
      if (TargetTags == null || TargetTags.Length == 0) {
        Main.Logger.LogError($"[ArtilleryByTagResult] No target tags specified for artillery strike '{Name}'");
        return;
      }

      // Get all combatants with the specified tags
      List<ICombatant> taggedCombatants = ObjectiveGameLogic.GetTaggedCombatants(combat, new TagSet(TargetTags));

      if (taggedCombatants == null || taggedCombatants.Count == 0) {
        Main.Logger.LogWarning($"[ArtilleryByTagResult] No combatants found with tags: {String.Join(", ", TargetTags)}");
        return;
      }

      Main.LogDebug($"[ArtilleryByTagResult] Found {taggedCombatants.Count} combatants with tags: {String.Join(", ", TargetTags)}");

      // Filter by TargetPriority
      if (TargetPriority != ArtilleryTargetPriority.None) {
        List<ICombatant> filteredCombatants = new List<ICombatant>();

        foreach (ICombatant combatant in taggedCombatants) {
          if (TargetPriority == ArtilleryTargetPriority.Units) {
            // Include only units (AbstractActor: mechs, vehicles, turrets)
            if (combatant is AbstractActor) {
              filteredCombatants.Add(combatant);
            }
          } else if (TargetPriority == ArtilleryTargetPriority.Buildings) {
            // Include only buildings
            if (combatant is BattleTech.Building) {
              filteredCombatants.Add(combatant);
            }
          }
        }

        if (filteredCombatants.Count > 0) {
          // Use priority targets if found
          taggedCombatants = filteredCombatants;
          Main.LogDebug($"[ArtilleryByTagResult] After filtering by TargetPriority '{TargetPriority}': {taggedCombatants.Count} priority combatants found");
        } else {
          // Fall back to all tagged combatants if no priority targets found
          Main.LogDebug($"[ArtilleryByTagResult] No priority targets found for '{TargetPriority}', falling back to all {taggedCombatants.Count} tagged combatants");
        }
      }

      // Determine which combatants to engage
      List<ICombatant> combatantsToEngage = new List<ICombatant>();

      if (ShotsFireAtAllTargets) {
        combatantsToEngage.AddRange(taggedCombatants);
        Main.LogDebug($"[ArtilleryByTagResult] Engaging all {combatantsToEngage.Count} targets");
      } else {
        // Pick a random target using combat-safe random to avoid deterministic selection
        int randomIndex = RandomUtils.GetCombatSafeRandomIndex(combat, taggedCombatants.Count);
        combatantsToEngage.Add(taggedCombatants[randomIndex]);
        Main.LogDebug($"[ArtilleryByTagResult] Engaging 1 random target (index {randomIndex})");
      }

      // Edge case: If ChanceToHit is 0, all shots will miss
      if (ChanceToHit <= 0.0f) {
        Main.LogDebug($"[ArtilleryByTagResult] ChanceToHit is 0, all shots will miss");

        foreach (ICombatant combatant in combatantsToEngage) {
          Vector3 position = combatant.CurrentPosition.GetLerpedHeightAt();
          Vector3 missPosition = CalculateMissPosition(position);
          LaunchSingleArtilleryStrike(missPosition, null); // No damage
        }

        Main.LogDebug($"[ArtilleryByTagResult] Artillery strike complete: 0 hits, {combatantsToEngage.Count} misses");
        return;
      }

      // Edge case: If ChanceToHit is 1, all shots will hit (no need to roll)
      if (ChanceToHit >= 1.0f) {
        Main.LogDebug("[ArtilleryByTagResult] ChanceToHit is 1, all shots will hit");

        foreach (ICombatant combatant in combatantsToEngage) {
          Vector3 position = combatant.CurrentPosition.GetLerpedHeightAt();
          LaunchSingleArtilleryStrike(position, combatant); // With damage
        }

        Main.LogDebug($"[ArtilleryByTagResult] Artillery strike complete: {combatantsToEngage.Count} hits, 0 misses");
        return;
      }

      // Roll for hits and misses, launching individual staggered sequences
      int hitCount = 0;
      int missCount = 0;

      foreach (ICombatant combatant in combatantsToEngage) {
        float roll = UnityEngine.Random.value; // Returns 0.0 to 1.0
        bool isHit = roll <= ChanceToHit;

        Vector3 position = combatant.CurrentPosition.GetLerpedHeightAt();

        if (isHit) {
          // Hit: Launch artillery at exact position with damage
          LaunchSingleArtilleryStrike(position, combatant);
          hitCount++;
          Main.LogDebug($"[ArtilleryByTagResult] Target '{combatant.DisplayName}' - HIT (rolled {roll:F2} vs {ChanceToHit:F2})");
        } else {
          // Miss: Launch artillery at offset position without damage
          Vector3 missPosition = CalculateMissPosition(position);
          LaunchSingleArtilleryStrike(missPosition, null);
          missCount++;
          Main.LogDebug($"[ArtilleryByTagResult] Target '{combatant.DisplayName}' - MISS (rolled {roll:F2} vs {ChanceToHit:F2})");
        }
      }

      Main.LogDebug($"[ArtilleryByTagResult] Artillery strike complete: {hitCount} hits, {missCount} misses - {hitCount + missCount} total sequences enqueued");
    }

    private void LaunchSingleArtilleryStrike(Vector3 position, ICombatant target) {
      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;

      // Create lists with single position and optional target
      List<Vector3> positions = new List<Vector3> { position };
      List<ICombatant> targets = new List<ICombatant>();

      if (target != null) {
        // Hit: Include target for damage
        targets.Add(target);
      }
      // Else: Miss - empty target list means no damage

      // Create individual artillery sequence
      // Each sequence will play independently, creating a staggered effect
      // when multiple sequences are enqueued in order
      ArtilleryObjectiveSequence artillerySequence = new ArtilleryObjectiveSequence(
        combat,
        positions,
        ArtilleryVFXType,
        targets,            // Empty for misses, single target for hits
        (float)Damage,
        HeatDamage,
        StabilityDamage,
        TerrainMaskFlags.None
      );

      EncounterLayerParent.EnqueueLoadAwareMessage(new AddSequenceToStackMessage(artillerySequence));
    }

    private Vector3 CalculateMissPosition(Vector3 targetPosition) {
      // Generate a random angle (0 to 360 degrees)
      float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

      // Calculate random distance with variance
      float randomDistance = MISS_DISTANCE + UnityEngine.Random.Range(-MISS_DISTANCE_VARIANCE, MISS_DISTANCE_VARIANCE);

      // Ensure distance is positive
      randomDistance = Mathf.Max(randomDistance, 1.0f);

      // Calculate offset in X and Z
      float offsetX = Mathf.Cos(randomAngle) * randomDistance;
      float offsetZ = Mathf.Sin(randomAngle) * randomDistance;

      // Apply offset to target position
      Vector3 missPosition = new Vector3(
        targetPosition.x + offsetX,
        targetPosition.y, // Will be recalculated for terrain height
        targetPosition.z + offsetZ
      );

      // Get terrain height for the miss position
      return missPosition.GetLerpedHeightAt();
    }
  }
}
