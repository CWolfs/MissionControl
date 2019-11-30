using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class ExtendedBoundariesOverride {
    [JsonProperty("MapId")]
    public string MapId { get; set; } = "UNSET";

    [JsonProperty("ContractTypeName")]
    public string ContractTypeName { get; set; } = "UNSET";

    [JsonProperty("SizePercentage")]
    public float IncreaseBoundarySizeByPercentage { get; set; } = 0.2f;

    public string Id {
      get {
        return $"{MapId}.{ContractTypeName}";
      }
    }
  }
}