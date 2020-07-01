using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class AdvancedSettings {
    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("EnableForFlashpoints")]
    public bool EnableForFlashpoints { get; set; } = true;

    [JsonProperty("IncludeContractTypes")]
    public List<string> IncludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("ExcludeContractTypes")]
    public List<string> ExcludeContractTypes { get; set; } = new List<string>();

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

      Main.Logger.Log($"[{this.GetType().Name}] Valid contracts are '{string.Join(", ", validContracts.ToArray())}'");
      return validContracts;
    }
  }
}