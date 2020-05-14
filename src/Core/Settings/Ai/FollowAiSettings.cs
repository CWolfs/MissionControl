using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class FollowAiSettings {
    [JsonProperty("Pathfinding")]
    public string Pathfinding { get; set; } = "Alternative"; // Original, Alternative

    [JsonProperty("Target")]
    public string Target { get; set; } = "HeaviestMech";    // HeaviestMech, FirstLanceMember

    [JsonProperty("StopWhen")]
    public string StopWhen { get; set; } = "OnEnemyDetected";    // OnEnemyDetected, OnEnemyVisible, WhenNotNeeded

    [JsonProperty("MaxDistanceFromTargetBeforeSprinting")]
    public float MaxDistanceFromTargetBeforeSprinting { get; set; } = 200;

    [JsonProperty("TargetZoneRadius")]
    public float TargetZoneRadius { get; set; } = 120;
  }
}