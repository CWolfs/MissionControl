using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class AiSettings : AdvancedSettings {
    [JsonProperty("FollowPlayer")]
    public FollowAiSettings FollowAiSettings { get; set; } = new FollowAiSettings();
  }
}