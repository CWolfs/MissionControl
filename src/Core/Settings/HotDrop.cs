using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class HotDrop {
    [JsonProperty("Enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("GuardOnHotDrop")]
    public bool GuardOnHotDrop { get; set; } = false;

    [JsonProperty("EvasionPipsOnHotDrop")]
    public int EvasionPipsOnHotDrop { get; set; } = 4;

    [JsonProperty("IncludeEnemies")]
    public bool IncludeEnemies { get; set; } = true;

    [JsonProperty("IncludeAllyTurrets")]
    public bool IncludeAllyTurrets { get; set; } = false;

    [JsonProperty("IncludeEnemyTurrets")]
    public bool IncludeEnemyTurrets { get; set; } = false;
  }
}