using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class ExtendedBoundariesSettings : AdvancedSettings {
    [JsonProperty("IncreaseBoundarySizeByPercentage")]
    public float IncreaseBoundarySizeByPercentage { get; set; } = 0.2f;

    [JsonProperty("Overrides")]
    public List<ExtendedBoundariesOverride> Overrides { get; set; } = new List<ExtendedBoundariesOverride>();

    public float GetSizePercentage(string mapId, string contractTypeName) {
      string id = $"{mapId}.{contractTypeName}";

      foreach (ExtendedBoundariesOverride ovr in Overrides) {
        if (ovr.Id == id) return ovr.IncreaseBoundarySizeByPercentage;
      }

      return IncreaseBoundarySizeByPercentage;
    }
  }
}