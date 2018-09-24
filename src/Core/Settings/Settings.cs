using Newtonsoft.Json;

namespace MissionControl {
    public class Settings {
        [JsonProperty("AdditionalLances")]
        public AdditionalLances AdditionalLances { get; set; } = new AdditionalLances();
    }
}