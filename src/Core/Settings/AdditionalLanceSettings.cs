using System.Collections.Generic;

using Newtonsoft.Json;

using System.Linq;

namespace MissionControl.Config {
  public class AdditionalLanceSettings {
    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("IncludeContractTypes")]
    public List<string> IncludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("ExcludeContractTypes")]
    public List<string> ExcludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("UseElites")]
    public bool UseElites { get; set; } = true;

    [JsonProperty("UseDialogue")]
    public bool UseDialogue { get; set; } = true;

    [JsonProperty("SkullValueMatters")]
    public bool SkullValueMatters { get; set; } = true;

    [JsonProperty("BasedOnVisibleSkullValue")]
    public bool BasedOnVisibleSkullValue { get; set; } = true;

    [JsonProperty("UseGeneralProfileForSkirmish")]
    public bool UseGeneralProfileForSkirmish { get; set; } = true;

    [JsonProperty("DisableIfFlashpointContract")]
    public bool DisableIfFlashpointContract { get; set; } = true;

    [JsonProperty("DisableWhenMaxTonnage")]
    public DisableWhenMaxTonnage DisableWhenMaxTonnage { get; set; } = new DisableWhenMaxTonnage();

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