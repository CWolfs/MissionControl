using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
	public class AdditionalLanceSettings {
		[JsonProperty("Enable")]
		public bool Enable { get; set; } = true;                                                                                                                                                                                                                                                                                                                                                           

		[JsonProperty("SkullValueMatters")]
		public bool SkullValueMatters { get; set; } = true;

		[JsonProperty("BasedOnVisibleSkullValue")]
		public bool BasedOnVisibleSkullValue { get; set; } = true;
	}
}