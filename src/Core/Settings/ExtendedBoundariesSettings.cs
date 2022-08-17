using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class ExtendedBoundariesSettings : AdvancedSettings {
    [JsonProperty("IncreaseBoundarySizeByPercentage")]
    public float IncreaseBoundarySizeByPercentage { get; set; } = 0.2f;

    [JsonProperty("Overrides")]
    public List<ExtendedBoundariesOverride> Overrides { get; set; } = new List<ExtendedBoundariesOverride>();

    private bool hasSortedOverrides = false;

    public float GetSizePercentage(string mapId, string contractTypeName) {
      if (!hasSortedOverrides) {
        Overrides = Overrides.OrderBy(x => x.MapId != "UNSET" && x.ContractTypeName != "UNSET").ToList();
        hasSortedOverrides = true;
      }

      string id = $"{mapId}.{contractTypeName}";

      foreach (ExtendedBoundariesOverride ovr in Overrides) {
        // Allow for fuzzy map matching
        if (ovr.MapId == "UNSET") ovr.MapId = mapId;
        if (ovr.ContractTypeName == "UNSET") ovr.ContractTypeName = contractTypeName;

        if (ovr.Id == id) return ovr.IncreaseBoundarySizeByPercentage;
      }

      return IncreaseBoundarySizeByPercentage;
    }
  }
}