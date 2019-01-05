using System.Collections.Generic;

using Newtonsoft.Json;

using BattleTech;

namespace MissionControl.Config {
	public class DisableWhenMaxTonnage {
    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("Limited")]
    public bool Limited { get; set; } = false;

    [JsonProperty("LimitedToUnder")]
    public int LimitedToUnder { get; set; } = 300;

    public bool AreLancesAllowed(int contractMaxTonnage) {
      if (!Enable) return true;

      if (Limited && contractMaxTonnage > -1) return false;

      if ((contractMaxTonnage == -1) || contractMaxTonnage < LimitedToUnder) return false;

      return true;
    }
  }
}