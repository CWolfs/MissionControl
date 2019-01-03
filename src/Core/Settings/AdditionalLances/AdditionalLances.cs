using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class AdditionalLances {
		[JsonProperty("IncludeContractTypes")]
		public List<string> IncludeContractTypes { get; set; } = new List<string>();

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

		public List<string> GetLancePoolKeys(string teamType, string biome, string contractType, string faction) {
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

			lancePoolKeys.AddRange(GetLancePoolKeys(LancePool, teamType, biome, contractType, faction));
			if (teamLancePool != null) lancePoolKeys.AddRange(GetLancePoolKeys(teamLancePool, teamType, biome, contractType, faction));
			
			return lancePoolKeys.Distinct().ToList();
		}

		private List<string> GetLancePoolKeys(Dictionary<string, List<string>> lancePool, string teamType, string biome, string contractType, string faction) {
			List<string> lancePoolKeys = new List<string>();
			string allIdentifier = "ALL";
			string biomeIdentifier = $"BIOME:{biome}";
			string contractTypeIdentifier = $"CONTRACT_TYPE:{contractType}";
			string factionIdentifier = $"FACTION:{faction}";

			if (lancePool.ContainsKey(allIdentifier)) lancePoolKeys.AddRange(lancePool[allIdentifier]);
			if (lancePool.ContainsKey(biomeIdentifier)) lancePoolKeys.AddRange(lancePool[biomeIdentifier]);
			if (lancePool.ContainsKey(contractTypeIdentifier)) lancePoolKeys.AddRange(lancePool[contractTypeIdentifier]);
			if (lancePool.ContainsKey(factionIdentifier)) lancePoolKeys.AddRange(lancePool[factionIdentifier]);

			return lancePoolKeys;
		}

		public List<string> GetValidContractTypes(string teamType) {
			List<string> validContracts = new List<string>();
			bool useInclude = false;
			bool useExclude = false;

			if (ExcludeContractTypes.Count > 0) {
				useExclude = true;
			}

			if (IncludeContractTypes.Count > 0) {
				useExclude = false;
				useInclude = true;
			}

			if (useInclude) {
				validContracts.AddRange(IncludeContractTypes);
			} else {
				validContracts.AddRange(MissionControl.Instance.GetAllContractTypes());
			}

			if (useExclude) {
				validContracts = validContracts.Except(ExcludeContractTypes).ToList();
			}

			if (teamType.ToLower() == "enemy") {
				if (Enemy.ExcludeContractTypes.Count > 0) validContracts = validContracts.Except(Enemy.ExcludeContractTypes).ToList();
			} else if (teamType.ToLower() == "allies") {
				if (Allies.ExcludeContractTypes.Count > 0) validContracts = validContracts.Except(Allies.ExcludeContractTypes).ToList();
			}

			Main.Logger.Log($"[GetValidContractTypes] Valid contracts are '{string.Join(", ", validContracts.ToArray())}'");
			return validContracts;
		}
	}
}