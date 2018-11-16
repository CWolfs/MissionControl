using System.Collections.Generic;

using Newtonsoft.Json;

using HBS.Collections;

namespace MissionControl.Data {
  public class MCastFirstNames {
    [JsonProperty("All")]
    public Dictionary<string, List<string>> All { get; set; } = new Dictionary<string, List<string>>();

    [JsonProperty("Male")]
    public Dictionary<string, List<string>>  Male { get; set; } = new Dictionary<string, List<string>>();

    [JsonProperty("Female")]
    public Dictionary<string, List<string>>  Female { get; set; } = new Dictionary<string, List<string>>();
  }
}