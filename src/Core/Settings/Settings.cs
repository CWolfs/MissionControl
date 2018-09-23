using Newtonsoft.Json;

namespace ContractCommand {
    public class Settings {
        [JsonProperty("AdditionalLances")]
        public AdditionalLances AdditionalLances { get; set; } = new AdditionalLances();
    }
}