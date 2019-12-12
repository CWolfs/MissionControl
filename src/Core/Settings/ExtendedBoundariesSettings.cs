using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class ExtendedBoundariesSettings {

    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("IncludeContractTypes")]
    public List<string> IncludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("ExcludeContractTypes")]
    public List<string> ExcludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("SizePercentage")]
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

    public List<string> GetValidContractTypes() {
      List<string> validContracts = new List<string>();
      bool useInclude = false;
      bool useExclude = false;

      if (ExcludeContractTypes.Count > 0) {
        useExclude = true;
      }

      if (IncludeContractTypes.Count > 0) {
        useExclude = false;
        useInclude = true;
      }

      if (useInclude) {
        validContracts.AddRange(IncludeContractTypes);
      } else {
        validContracts.AddRange(MissionControl.Instance.GetAllContractTypes());
      }

      if (useExclude) {
        validContracts = validContracts.Except(ExcludeContractTypes).ToList();
      }

      Main.Logger.Log($"[ExtendedBoundaries.GetValidContractTypes] Valid contracts are '{string.Join(", ", validContracts.ToArray())}'");
      return validContracts;
    }
  }
}