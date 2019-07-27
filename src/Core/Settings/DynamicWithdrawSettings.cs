using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class DynamicWithdrawSettings {
		[JsonProperty("Enable")]
		public bool Enable { get; set; } = true;

		[JsonProperty("OnWithdrawButton")]
		public bool OnWithdrawButton { get; set; } = true;
	}
}