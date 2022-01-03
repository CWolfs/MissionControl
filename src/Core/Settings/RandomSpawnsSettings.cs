using Newtonsoft.Json;

namespace MissionControl.Config {
  public class RandomSpawnsSettings : AdvancedSettings {
    [JsonProperty("TimeoutIn")]
    public int TimeoutIn { get; set; } = 30; // Seconds
  }
}