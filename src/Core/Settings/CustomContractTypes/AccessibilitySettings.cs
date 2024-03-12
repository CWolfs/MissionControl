using Newtonsoft.Json;

namespace MissionControl.Config {
  public class AccessibilitySettings {
    [JsonProperty("AllowCameraShake")]
    public bool AllowCameraShake { get; set; } = true;
  }
}