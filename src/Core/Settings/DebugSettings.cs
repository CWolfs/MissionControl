using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class DebugSettings : AdvancedSettings {
    [JsonProperty("AdditionalLancesEnemyLanceCount")]
    public int AdditionalLancesEnemyLanceCount { get; set; } = -1;
  }
}