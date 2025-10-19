using System;

using BattleTech;

namespace MissionControl.Utils {
  public static class RandomUtils {
    /// <summary>
    /// Gets a random index that's safe to use even when Unity's Random seed is deterministic.
    /// Combines high-resolution time and combat state for better entropy.
    /// </summary>
    /// <param name="combat">Current combat game state</param>
    /// <param name="maxValue">Maximum value (exclusive)</param>
    /// <returns>Random index between 0 and maxValue-1</returns>
    public static int GetCombatSafeRandomIndex(CombatGameState combat, int maxValue) {
      if (maxValue <= 0) {
        throw new ArgumentException("maxValue must be greater than 0", nameof(maxValue));
      }

      if (maxValue == 1) {
        return 0;
      }

      // Use high-resolution time for entropy
      int timeSeed = (int)(DateTime.Now.Ticks % int.MaxValue);

      // Add combat state for additional variability
      int roundSeed = combat.TurnDirector.CurrentRound * 1000 + combat.TurnDirector.CurrentPhase;

      // Combine the seeds
      int combinedSeed = timeSeed ^ roundSeed;

      // Return a valid index
      return Math.Abs(combinedSeed) % maxValue;
    }
  }
}
