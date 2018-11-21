using Newtonsoft.Json;

namespace MissionControl.Config {
    public class Settings {
        [JsonProperty("DebugMode")]
        public bool DebugMode { get; set; } = false;

        [JsonProperty("DebugSkirmishMode")]
        public bool DebugSkirmishMode { get; set; } = false;

        public AdditionalLances AdditionalLances { get; set; } = new AdditionalLances();
    }
}