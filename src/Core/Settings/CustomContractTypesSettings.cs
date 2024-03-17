using Newtonsoft.Json;

namespace MissionControl.Config {
  public class CustomContractTypesSettings {
    [JsonProperty("DeveloperMode")]
    public bool DeveloperMode { get; set; } = false;

    [JsonProperty("Accessibility")]
    public AccessibilitySettings AccessibilitySettings { get; set; } = new AccessibilitySettings();
  }
}