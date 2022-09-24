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
        Overrides = Overrides.OrderBy(x => x.MapId != "UNSET" && x.ContractTypeName != "UNSET").
          ThenBy(x => x.MapId != "UNSET" && x.ContractTypeName == "UNSET").
          ThenBy(x => x.MapId == "UNSET" && x.ContractTypeName != "UNSET").ToList();
        hasSortedOverrides = true;
      }

      string id = $"{mapId}.{contractTypeName}";

      foreach (ExtendedBoundariesOverride ovr in Overrides) {
        string builtMapId = ovr.MapId;
        string builtContractTypeName = ovr.ContractTypeName;

        // Allow for fuzzy map matching
        if (builtMapId == "UNSET") builtMapId = mapId;
        if (builtContractTypeName == "UNSET") builtContractTypeName = contractTypeName;

        string builtId = $"{builtMapId}.{builtContractTypeName}";
        if (id == builtId) return ovr.IncreaseBoundarySizeByPercentage;
      }

      return IncreaseBoundarySizeByPercentage;
    }
  }
}