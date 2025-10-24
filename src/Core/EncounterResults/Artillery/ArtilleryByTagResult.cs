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
    public float SplashRange { get; set; }
    public bool SplashRequiresTags { get; set; }
    public float VFXScale { get; set; } = 1f;

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[ArtilleryByTagResult] Triggering artillery strike '{Name}' - {Description}");

      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;

      List<ICombatant> taggedCombatants = ValidateAndGetTaggedCombatants(combat);
      if (taggedCombatants == null) return;

      // Filter dead
      taggedCombatants.RemoveAll(c => c.IsDead);
      if (taggedCombatants.Count == 0) {
        Main.Logger.Log($"[ArtilleryByTagResult] No valid (alive) combatants found with tags: {String.Join(", ", TargetTags)}");
        return;
      }

      // Only apply priority filtering if we're selecting a single random target
      // If ShotsFireAtAllTargets is true, we should engage ALL tagged combatants
      List<ICombatant> filteredCombatants = ShotsFireAtAllTargets
        ? taggedCombatants
        : FilterTargetsByPriority(taggedCombatants);
      List<ICombatant> combatantsToEngage = SelectTargetsToEngage(combat, filteredCombatants);

      ExecuteArtilleryStrikes(combatantsToEngage);
    }

    private List<ICombatant> ValidateAndGetTaggedCombatants(CombatGameState combat) {
      // Validate ChanceToHit
      if (ChanceToHit < 0.0f || ChanceToHit > 1.0f) {
        Main.Logger.LogError($"[ArtilleryByTagResult] ChanceToHit must be between 0.0 and 1.0. Got: {ChanceToHit}. Clamping to valid range.");
        ChanceToHit = Mathf.Clamp01(ChanceToHit);
      }

      // Validate TargetTags
      if (TargetTags == null || TargetTags.Length == 0) {
        Main.Logger.LogError($"[ArtilleryByTagResult] No target tags specified for artillery strike '{Name}'");
        return null;
      }

      // Get all combatants with the specified tags
      List<ICombatant> taggedCombatants = ObjectiveGameLogic.GetTaggedCombatants(combat, new TagSet(TargetTags));

      if (taggedCombatants == null || taggedCombatants.Count == 0) {
        Main.Logger.LogWarning($"[ArtilleryByTagResult] No combatants found with tags: {String.Join(", ", TargetTags)}");
        return null;
      }

      Main.LogDebug($"[ArtilleryByTagResult] Found {taggedCombatants.Count} combatants with tags: {String.Join(", ", TargetTags)}");
      taggedCombatants.Shuffle();
      return taggedCombatants;
    }

    private List<ICombatant> FilterTargetsByPriority(List<ICombatant> taggedCombatants) {
      // No filtering needed if priority is None
      if (TargetPriority == ArtilleryTargetPriority.None) {
        return taggedCombatants;
      }

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
        Main.LogDebug($"[ArtilleryByTagResult] After filtering by TargetPriority '{TargetPriority}': {filteredCombatants.Count} priority combatants found");
        return filteredCombatants;
      } else {
        // Fall back to all tagged combatants if no priority targets found
        Main.LogDebug($"[ArtilleryByTagResult] No priority targets found for '{TargetPriority}', falling back to all {taggedCombatants.Count} tagged combatants");
        return taggedCombatants;
      }
    }

    private List<ICombatant> SelectTargetsToEngage(CombatGameState combat, List<ICombatant> availableTargets) {
      List<ICombatant> combatantsToEngage = new List<ICombatant>();

      if (ShotsFireAtAllTargets) {
        combatantsToEngage.AddRange(availableTargets);
        Main.LogDebug($"[ArtilleryByTagResult] Engaging all {combatantsToEngage.Count} targets");
      } else {
        // Pick a random target using combat-safe random to avoid deterministic selection
        int randomIndex = RandomUtils.GetCombatSafeRandomIndex(combat, availableTargets.Count);
        combatantsToEngage.Add(availableTargets[randomIndex]);
        Main.LogDebug($"[ArtilleryByTagResult] Engaging 1 random target (index {randomIndex})");
      }

      return combatantsToEngage;
    }

    private void ExecuteArtilleryStrikes(List<ICombatant> combatantsToEngage) {
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

      // Create individual artillery sequence with VFX scaling
      // Each sequence will play independently, creating a staggered effect
      // when multiple sequences are enqueued in order
      ScalableArtilleryObjectiveSequence artillerySequence = new ScalableArtilleryObjectiveSequence(
        combat,
        positions,
        ArtilleryVFXType,
        targets,            // Empty for misses, single target for hits
        Damage,
        HeatDamage,
        StabilityDamage,
        TerrainMaskFlags.None,
        VFXScale
      );

      EncounterLayerParent.EnqueueLoadAwareMessage(new AddSequenceToStackMessage(artillerySequence));

      // Apply splash damage if configured
      if (SplashRange > 0f) {
        Main.LogDebug($"[ArtilleryByTagResult] Splash damage enabled - Range: {SplashRange}, RequiresTags: {SplashRequiresTags}");
        ApplySplashDamage(position, target);
      } else {
        Main.LogDebug($"[ArtilleryByTagResult] Splash damage skipped - Target is null: {target == null}, SplashRange: {SplashRange}");
      }
    }

    private void ApplySplashDamage(Vector3 position, ICombatant primaryTarget) {
      Main.LogDebug($"[ArtilleryByTagResult] ApplySplashDamage called for primary target '{primaryTarget.DisplayName}' at position {position}");

      // Get all combatants in splash range
      List<ICombatant> splashTargets = GetCombatantsInSplashRange(position, primaryTarget);

      if (splashTargets.Count == 0) {
        Main.LogDebug("[ArtilleryByTagResult] No combatants found in splash range");
        return;
      }

      Main.LogDebug($"[ArtilleryByTagResult] Found {splashTargets.Count} potential splash targets before tag filtering");

      // Filter by tags if required
      splashTargets = FilterSplashTargetsByTags(splashTargets);

      if (splashTargets.Count == 0) {
        Main.LogDebug("[ArtilleryByTagResult] No valid splash targets after filtering");
        return;
      }

      // Calculate splash damage (half of main damage)
      int splashDamage = Damage / 2;
      int splashHeatDamage = HeatDamage / 2;
      int splashStabilityDamage = StabilityDamage / 2;

      Main.LogDebug($"[ArtilleryByTagResult] Applying splash damage to {splashTargets.Count} targets: Damage={splashDamage}, Heat={splashHeatDamage}, Stability={splashStabilityDamage}");

      // Apply damage directly to splash targets without visual effects
      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;

      foreach (ICombatant splashTarget in splashTargets) {
        Main.LogDebug($"[ArtilleryByTagResult] Applying splash damage to '{splashTarget.DisplayName}'");

        if (splashDamage > 0) {
          DamageOrderUtility.ApplyDamageToAllLocations("ArtillerySplash", combat.StackManager.NextStackUID, combat.StackManager.NextStackUID, splashTarget, splashDamage, splashDamage, AttackDirection.FromArtillery, DamageType.Artillery);
        }

        if (splashHeatDamage > 0) {
          DamageOrderUtility.ApplyHeatDamage(combat.StackManager.NextStackUID, splashTarget, splashHeatDamage);
        }

        if (splashStabilityDamage > 0) {
          DamageOrderUtility.ApplyStabilityDamage(combat.StackManager.NextStackUID, splashTarget, splashStabilityDamage);
        }
      }
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

    private List<ICombatant> GetCombatantsInSplashRange(Vector3 position, ICombatant primaryTarget) {
      if (SplashRange <= 0f) {
        Main.LogDebug("[ArtilleryByTagResult] SplashRange is 0 or negative, returning empty list");
        return new List<ICombatant>();
      }

      Main.LogDebug($"[ArtilleryByTagResult] Searching for combatants within {SplashRange} meters of position {position}");

      CombatGameState combat = UnityGameInstance.BattleTechGame.Combat;
      List<ICombatant> splashTargets = new List<ICombatant>();

      // Get all living combatants in the combat
      List<ICombatant> allLivingCombatants = combat.GetAllLivingCombatants();
      Main.LogDebug($"[ArtilleryByTagResult] Total living combatants in combat: {allLivingCombatants.Count}");

      foreach (ICombatant combatant in allLivingCombatants) {
        // Skip the primary target (already taking full damage)
        if (combatant == primaryTarget) {
          Main.LogDebug($"[ArtilleryByTagResult] Skipping primary target '{combatant.DisplayName}'");
          continue;
        }

        // Check if combatant is within splash range
        float distance = Vector3.Distance(position, combatant.CurrentPosition);
        if (distance <= SplashRange) {
          splashTargets.Add(combatant);
          Main.LogDebug($"[ArtilleryByTagResult] '{combatant.DisplayName}' is within range at distance {distance:F2}m");
        } else {
          Main.LogDebug($"[ArtilleryByTagResult] '{combatant.DisplayName}' is out of range at distance {distance:F2}m");
        }
      }

      Main.LogDebug($"[ArtilleryByTagResult] Found {splashTargets.Count} combatants within splash range {SplashRange}");
      return splashTargets;
    }

    private List<ICombatant> FilterSplashTargetsByTags(List<ICombatant> splashTargets) {
      if (!SplashRequiresTags || TargetTags == null || TargetTags.Length == 0) {
        Main.LogDebug($"[ArtilleryByTagResult] Tag filtering disabled - SplashRequiresTags: {SplashRequiresTags}, TargetTags: {(TargetTags == null ? "null" : TargetTags.Length.ToString())}");
        return splashTargets;
      }

      Main.LogDebug($"[ArtilleryByTagResult] Filtering splash targets by tags: {String.Join(", ", TargetTags)}");

      List<ICombatant> filteredTargets = new List<ICombatant>();
      TagSet requiredTags = new TagSet(TargetTags);

      foreach (ICombatant combatant in splashTargets) {
        bool hasTags = combatant.EncounterTags.ContainsAll(requiredTags);
        if (hasTags) {
          filteredTargets.Add(combatant);
          Main.LogDebug($"[ArtilleryByTagResult] '{combatant.DisplayName}' has required tags - included");
        } else {
          Main.LogDebug($"[ArtilleryByTagResult] '{combatant.DisplayName}' missing required tags - excluded");
        }
      }

      Main.LogDebug($"[ArtilleryByTagResult] After tag filtering: {filteredTargets.Count} splash targets (from {splashTargets.Count})");
      return filteredTargets;
    }
  }
}
