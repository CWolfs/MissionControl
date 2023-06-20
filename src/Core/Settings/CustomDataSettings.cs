using Newtonsoft.Json;

namespace MissionControl.Config {
  public class CustomDataSettings {
    [JsonProperty("Search")]
    public bool Search { get; set; } = true;

    [JsonProperty("SearchType")]
    public string SearchType { get; set; } = "ShallowFromModsFolder";  // DeepFromModsFolder
  }
}