using Newtonsoft.Json;

namespace MissionControl {
    public class AdditionalLances {
        [JsonProperty("Min")]
        public int Min { get; set; } = 0;

        [JsonProperty("Max")]
        public int Max { get; set; } = 0;

        public int SelectNumberOfAdditionalEnemyLances() {
            int lanceNumber = UnityEngine.Random.Range(Min, Max);
            Main.Logger.Log($"[SelectNumberOfAdditionalEnemyLances] {lanceNumber}");
            return lanceNumber;
        }
    }
}