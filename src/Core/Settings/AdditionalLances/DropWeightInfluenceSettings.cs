using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class DropWeightInfluenceSettings {
    [JsonProperty("Enable")]
    public bool Enable { get; set; } = false;

    [JsonProperty("EnemySpawnInfluencePerHalfSkullAbove")]
    public float EnemySpawnInfluencePerHalfSkullAbove { get; set; } = 0.1f;

    [JsonProperty("AllySpawnInfluencePerHalfSkullAbove")]
    public float AllySpawnInfluencePerHalfSkullAbove { get; set; } = -0.1f;

    [JsonProperty("EnemySpawnInfluencePerHalfSkullBelow")]
    public float EnemySpawnInfluencePerHalfSkullBelow { get; set; } = 0.1f;

    [JsonProperty("AllySpawnInfluencePerHalfSkullBelow")]
    public float AllySpawnInfluencePerHalfSkullBelow { get; set; } = -0.1f;
  }
}