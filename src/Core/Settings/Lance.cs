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
			int lanceNumber = 0;
			int chanceToSpawn = (int)(ChanceToSpawn * 100f);

			for (int i = 0; i < Max; i++) {
				int rollChance = UnityEngine.Random.Range(1, 100);
				if (rollChance > chanceToSpawn) break;
				lanceNumber++;
			}

			Main.Logger.Log($"[SelectNumberOfAdditionalLances] {lanceNumber}");	
			return lanceNumber;
		}
	}
}