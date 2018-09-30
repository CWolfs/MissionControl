using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
    public class AdditionalLances {
        [JsonProperty("IncludeContractTypes")]
        public List<string> IncludeContractTypes { get; set; } = new List<string>(){ "ALL" };

        [JsonProperty("ExcludeContractTypes")]
        public List<string> ExcludeContractTypes { get; set; } = new List<string>();

        [JsonProperty("LancePool")]
        public Dictionary<string, List<string>> LancePool { get; set; } = new Dictionary<string, List<string>>();

        [JsonProperty("Player")]
        public Lance Player { get; set; } = new Lance();

        [JsonProperty("Enemy")]
        public Lance Enemy { get; set; } = new Lance();

        [JsonProperty("Allies")]
        public Lance Allies { get; set; } = new Lance();
    }
}