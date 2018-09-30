using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class AdditionalLances {
		[JsonProperty("IncludeContractTypes")]
		public List<string> IncludeContractTypes { get; set; } = new List<string>(){ "ALL" };

		[JsonProperty("ExcludeContractTypes")]
		public List<string> ExcludeContractTypes { get; set; } = new List<string>();

		[JsonProperty("LancePool")]
		public Dictionary<string, List<string>> LancePool { get; set; } = new Dictionary<string, List<string>>() {
			{ "ALL", new List<string>{"GENERIC_BATTLE_LANCE"} }
		};

		[JsonProperty("Player")]
		public Lance Player { get; set; } = new Lance();

		[JsonProperty("Enemy")]
		public Lance Enemy { get; set; } = new Lance();

		[JsonProperty("Allies")]
		public Lance Allies { get; set; } = new Lance();

		public List<string> GetLancePoolKeys(string teamType, string biome, string contractType) {
			List<string> lancePoolKeys = new List<string>();
			Dictionary<string, List<string>> teamLancePool = null;

			switch (teamType.ToLower()) {
				case "enemy":
					teamLancePool = Enemy.LancePool;
					break;
				case "allies":
					teamLancePool = Allies.LancePool;
					break;
			}

			lancePoolKeys.AddRange(GetLancePoolKeys(LancePool, teamType, biome, contractType));
			if (teamLancePool != null) lancePoolKeys.AddRange(GetLancePoolKeys(teamLancePool, teamType, biome, contractType));
			
			return lancePoolKeys.Distinct().ToList();
		}

		private List<string> GetLancePoolKeys(Dictionary<string, List<string>> lancePool, string teamType, string biome, string contractType) {
			List<string> lancePoolKeys = new List<string>();
			string allIdentifier = "ALL";
			string biomeIdentifier = $"BIOME:{biome}";
			string contractTypeIdentifier = $"CONTRACT_TYPE:{contractType}";

			if (lancePool.ContainsKey(allIdentifier)) lancePoolKeys.AddRange(lancePool[allIdentifier]);
			if (lancePool.ContainsKey(biomeIdentifier)) lancePoolKeys.AddRange(lancePool[biomeIdentifier]);
			if (lancePool.ContainsKey(contractTypeIdentifier)) lancePoolKeys.AddRange(lancePool[contractTypeIdentifier]);

			return lancePoolKeys;
		}
	}
}