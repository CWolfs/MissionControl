using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class SpawnerSettings {
    [JsonProperty("SpawnLanceAtEdgeBoundary")]
    public SpawnLanceAtBoundarySettings SpawnLanceAtBoundary { get; set; } = new SpawnLanceAtBoundarySettings();
  }
}