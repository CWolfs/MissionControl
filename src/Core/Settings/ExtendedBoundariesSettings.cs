using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class ExtendedBoundariesSettings {

    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("Size")]
    public string Size { get; set; } = "Medium";
  }
}