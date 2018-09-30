using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class Lance {
		[JsonProperty("Max")]
		public int Max { get; set; } = 0;

		[JsonProperty("ExcludeContractTypes")]
		public List<string> ExcludeContractTypes { get; set; } = new List<string>();

		[JsonProperty("ChanceToSpawn")]
		public float ChanceToSpawn = 0;

		[JsonProperty("LancePool")]
		public Dictionary<string, List<string>> LancePool { get; set; } = new Dictionary<string, List<string>>();

		public int SelectNumberOfAdditionalLances() {
			int lanceNumber = UnityEngine.Random.Range(1, Max);
			Main.Logger.Log($"[SelectNumberOfAdditionalLances] {lanceNumber}");
			return lanceNumber;
		}
	}
}