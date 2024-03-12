using Newtonsoft.Json;

namespace MissionControl.Config {
  public class CustomContractTypesSettings {
    [JsonProperty("Accessibility")]
    public AccessibilitySettings AccessibilitySettings { get; set; } = new AccessibilitySettings();
  }
}