using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class HotDrop {
    [JsonProperty("GuardOnHotDrop")]
    public bool GuardOnHotDrop { get; set; } = false;

    [JsonProperty("EvasionPipsOnHotDrop")]
    public int EvasionPipsOnHotDrop { get; set; } = 4;

    [JsonProperty("IncludeEnemies")]
    public bool IncludeEnemies { get; set; } = true;
  }
}