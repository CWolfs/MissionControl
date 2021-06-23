using Newtonsoft.Json;

namespace MissionControl.Config {
  public class LanceSelectionDivergenceOverride {
    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("Divergence")]
    public int Divergence { get; set; } = 20;
  }
}