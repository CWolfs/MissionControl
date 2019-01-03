using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class Lance {
		[JsonProperty("Max")]
		private int max = 0;
		[JsonIgnore]
		public int Max {
			get {
				return EliteLances.Overrides.ContainsKey("Max") ? int.Parse(EliteLances.Overrides["Max"]) : max;
			}
			set {
				max = value;
			}
		}

		[JsonProperty("ExcludeContractTypes")]
		public List<string> ExcludeContractTypes { get; set; } = new List<string>();

		[JsonProperty("ChanceToSpawn")]
		private float chanceToSpawn = 0;
		[JsonIgnore]
		public float ChanceToSpawn {
			get {
				return EliteLances.Overrides.ContainsKey("ChanceToSpawn") ? float.Parse(EliteLances.Overrides["ChanceToSpawn"]) : chanceToSpawn;
			}
			set {
				chanceToSpawn = value;
			}
		}

		[JsonProperty("EliteLances")]
		public EliteLances EliteLances = new EliteLances();

		[JsonProperty("LancePool")]
		public Dictionary<string, List<string>> LancePool { get; set; } = new Dictionary<string, List<string>>();

		public int SelectNumberOfAdditionalLances() {
			int lanceNumber = 0;
			int chanceToSpawn = (int)(ChanceToSpawn * 100f);

			for (int i = 0; i < Max; i++) {
				int rollChance = UnityEngine.Random.Range(1, 100 + 1);
				if (rollChance > chanceToSpawn) break;
				lanceNumber++;
			}

			Main.Logger.Log($"[SelectNumberOfAdditionalLances] {lanceNumber}");	
			return lanceNumber;
		}
	}
}