using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class DropWeightInfluenceSettings {
    [JsonProperty("Enable")]
    public bool Enable { get; set; } = false;

    [JsonProperty("GlobalEnemyChanceToSpawn")]
    public float GlobalEnemyChanceToSpawn { get; set; } = 0.3f;

    [JsonProperty("GlobalAllyChanceToSpawn")]
    public float GlobalAllyChanceToSpawn { get; set; } = 0.5f;

    [JsonProperty("EnemySpawnInfluencePerHalfSkull")]
    public float EnemySpawnInfluencePerHalfSkull { get; set; } = 0.1f;

    [JsonProperty("AllySpawnInfluencePerHalfSkull")]
    public float AllySpawnInfluencePerHalfSkull { get; set; } = -0.1f;
  }
}