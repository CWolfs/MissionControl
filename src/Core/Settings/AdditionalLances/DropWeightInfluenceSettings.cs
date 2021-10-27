using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class DropWeightInfluenceSettings {
    [JsonProperty("Enable")]
    public bool Enable { get; set; } = false;

    [JsonProperty("EnemySpawnInfluenceMax")]
    public float EnemySpawnInfluenceMax { get; set; } = 0.9f;

    [JsonProperty("AllySpawnInfluenceMax")]
    public float AllySpawnInfluenceMax { get; set; } = 0.9f;

    [JsonProperty("EnemySpawnInfluencePerHalfSkullAbove")]
    public float EnemySpawnInfluencePerHalfSkullAbove { get; set; } = 0.1f;

    [JsonProperty("AllySpawnInfluencePerHalfSkullAbove")]
    public float AllySpawnInfluencePerHalfSkullAbove { get; set; } = -0.1f;

    [JsonProperty("EnemySpawnInfluencePerHalfSkullBelow")]
    public float EnemySpawnInfluencePerHalfSkullBelow { get; set; } = 0.1f;

    [JsonProperty("AllySpawnInfluencePerHalfSkullBelow")]
    public float AllySpawnInfluencePerHalfSkullBelow { get; set; } = -0.1f;

    public float GetSpawnInfluenceMax(string team) {
      if (team == "Enemy") {
        return EnemySpawnInfluenceMax;
      } else if (team == "Allies") {
        return AllySpawnInfluenceMax;
      }

      Main.Logger.LogError($"[GetSpawnInfluenceMax] Team '{team}' is invalid. Must be 'Enemy' or 'Allies'. Returning 0.9");
      return 0.9f;
    }
  }
}