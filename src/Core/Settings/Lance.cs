using Newtonsoft.Json;

namespace MissionControl.Config {
	public class Lance {
		[JsonProperty("Max")]
		public int Max { get; set; } = 0;

		public int SelectNumberOfAdditionalLances() {
			int lanceNumber = UnityEngine.Random.Range(1, Max);
			Main.Logger.Log($"[SelectNumberOfAdditionalLances] {lanceNumber}");
			return lanceNumber;
		}
	}
}