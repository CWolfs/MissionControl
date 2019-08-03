using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class DynamicWithdrawSettings {
		[JsonProperty("Enable")]
		public bool Enable { get; set; } = true;

		[JsonProperty("DisorderlyWithdrawalCompatibility")]
		public bool DisorderlyWithdrawalCompatibility { get; set; } = false;

		[JsonProperty("OnWithdrawButton")]
		public bool FailUnfinishedObjectives { get; set; } = true;

		[JsonProperty("MinDistanceForZone")]
		public int MinDistanceForZone { get; set; } = 300;

		[JsonProperty("MaxDistanceForZone")]
		public int MaxDistanceForZone { get; set; } = 700;
	}
}