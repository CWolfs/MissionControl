using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class ExtendedBoundariesSettings {

    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("SizePercentage")]
    public float SizePercentage { get; set; } = 0.2f;

    [JsonProperty("Overrides")]
    public List<ExtendedBoundariesOverride> Overrides { get; set; } = new List<ExtendedBoundariesOverride>();

    public float GetSizePercentage(string mapId, string contractTypeName) {
      string id = $"{mapId}.{contractTypeName}";

      foreach (ExtendedBoundariesOverride ovr in Overrides) {
        if (ovr.Id == id) return ovr.SizePercentage;
      }

      return SizePercentage;
    }
  }
}