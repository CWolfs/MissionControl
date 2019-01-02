using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class ExtendedLancesSettings {
		[JsonProperty("Enable")]
		public bool Enable { get; set; } = true;

		[JsonProperty("Autofill")]
		public bool Autofill { get; set; } = true;

		[JsonProperty("LanceSizes")]
		public Dictionary<string, List<string>> LanceSizes { get; set; } = new Dictionary<string, List<string>>();

		public int GetFactionLanceSize(string factionKey) {
			foreach (KeyValuePair<string, List<string>> lanceSetPair in LanceSizes) {
				int lanceSize = int.Parse(lanceSetPair.Key);
				List<string> factions = lanceSetPair.Value;

				if (factions.Contains(factionKey)) return lanceSize;
			}

			return 4;
		}
	}
}