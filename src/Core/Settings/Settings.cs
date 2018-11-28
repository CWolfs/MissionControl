using Newtonsoft.Json;

using System.Collections.Generic;

namespace MissionControl.Config {
    public class Settings {
        [JsonProperty("DebugMode")]
        public bool DebugMode { get; set; } = false;

        [JsonProperty("DebugSkirmishMode")]
        public bool DebugSkirmishMode { get; set; } = false;

        [JsonProperty("HotDrop")]
        public HotDrop HotDrop { get; set; } = new HotDrop();

        [JsonProperty("AdditionalLances")]
        public AdditionalLanceSettings AdditionalLanceSettings { get; set; } = new AdditionalLanceSettings();

        public AdditionalLances ActiveAdditionalLances { get; set; }

        [JsonIgnore]
        public Dictionary<int, AdditionalLances> AdditionalLances { get; set; } = new Dictionary<int, AdditionalLances>();
    }
}