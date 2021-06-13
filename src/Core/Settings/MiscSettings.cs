using Newtonsoft.Json;

namespace MissionControl.Config {
  public class MiscSettings {
    [JsonProperty("LanceSelectionDivergenceOverride")]
    public LanceSelectionDivergenceOverride LanceSelectionDivergenceOverride { get; set; } = new LanceSelectionDivergenceOverride();
  }
}