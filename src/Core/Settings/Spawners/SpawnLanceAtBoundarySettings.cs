using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class SpawnLanceAtBoundarySettings {
    [JsonProperty("MinBuffer")]
    public float MinBuffer { get; set; } = 100;

    [JsonProperty("MaxBuffer")]
    public float MaxBuffer { get; set; } = 200;
  }
}