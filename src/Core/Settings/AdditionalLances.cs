using Newtonsoft.Json;

namespace MissionControl.Config {
    public class AdditionalLances {
        [JsonProperty("Player")]
        public Lance Player { get; set; } = new Lance();

        [JsonProperty("Enemy")]
        public Lance Enemy { get; set; } = new Lance();

        [JsonProperty("Allies")]
        public Lance Allies { get; set; } = new Lance();
    }
}