using Newtonsoft.Json;

namespace MissionControl.Config {
    public class Lance {
        [JsonProperty("Min")]
        public int Min { get; set; } = 0;

        [JsonProperty("Max")]
        public int Max { get; set; } = 0;

        public int SelectNumberOfAdditionalLances() {
            int lanceNumber = UnityEngine.Random.Range(Min, Max);
            Main.Logger.Log($"[SelectNumberOfAdditionalLances] {lanceNumber}");
            return lanceNumber;
        }
    }
}