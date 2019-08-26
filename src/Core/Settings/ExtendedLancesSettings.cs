using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class ExtendedLancesSettings {
		[JsonProperty("Enable")]
		public bool Enable { get; set; } = true;

		[JsonProperty("Autofill")]
		public bool Autofill { get; set; } = true;

		[JsonProperty("LanceSizes")]
		public Dictionary<string, List<ExtendedLance>> LanceSizes { get; set; } = new Dictionary<string, List<ExtendedLance>>();

		public int GetFactionLanceSize(string factionKey) {
			foreach (KeyValuePair<string, List<ExtendedLance>> lanceSetPair in LanceSizes) {
				int lanceSize = int.Parse(lanceSetPair.Key);
				List<ExtendedLance> factions = lanceSetPair.Value;

				ExtendedLance lance = factions.FirstOrDefault(extendedLance => extendedLance.Faction == factionKey);

				if (lance != null) return lanceSize;
			}

			return 4;
		}

		// public int GetFactionLanceDifficulty(string factionKey) {
			/*
			foreach (KeyValuePair<string, List<ExtendedLance>> lanceSetPair in LanceSizes) {
				int lanceSize = int.Parse(lanceSetPair.Key);
				List<ExtendedLance> factions = lanceSetPair.Value;

				ExtendedLance lance = factions.FirstOrDefault(extendedLance => extendedLance.Faction == factionKey);

				if (lance != null) return lanceSize;
			}

			return 4;
			*/
		// }
	}
}