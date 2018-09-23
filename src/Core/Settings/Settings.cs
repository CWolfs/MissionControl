using Newtonsoft.Json;

namespace EncounterCommand {
    public class Settings {
        [JsonProperty("AdditionalLances")]
        public AdditionalLances AdditionalLances { get; set; } = new AdditionalLances();
    }
}