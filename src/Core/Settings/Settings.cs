using Newtonsoft.Json;

namespace MissionControl.Config {
    public class Settings {
        [JsonProperty("AdditionalLances")]
        public AdditionalLances AdditionalLances { get; set; } = new AdditionalLances();
    }
}