using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class ExtendedLance {
    [JsonProperty("Faction")]
    public string Faction { get; set; }

    [JsonProperty("DifficultyMod")]
    public int DifficultyMod { get; set; } = 0;
  }
}