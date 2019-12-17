using System.Linq;
using System.Collections.Generic;

using BattleTech.Framework;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class ExtendedLancesSettings {
    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("IncludeContractTypes")]
    public List<string> IncludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("ExcludeContractTypes")]
    public List<string> ExcludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("Autofill")]
    public bool Autofill { get; set; } = true;

    [JsonProperty("LanceSizes")]
    public Dictionary<string, List<ExtendedLance>> LanceSizes { get; set; } = new Dictionary<string, List<ExtendedLance>>();

    public int GetFactionLanceSize(string factionKey) {
      foreach (KeyValuePair<string, List<ExtendedLance>> lanceSetPair in LanceSizes) {
        int lanceSize = int.Parse(lanceSetPair.Key);
        List<ExtendedLance> factions = lanceSetPair.Value;

        ExtendedLance lance = factions.FirstOrDefault(extendedLance => extendedLance.Faction == factionKey);

        if (lance != null) return lanceSize;
      }

      return 4;
    }

    public int GetFactionLanceDifficulty(string factionKey, LanceOverride lanceOverride) {
      foreach (KeyValuePair<string, List<ExtendedLance>> lanceSetPair in LanceSizes) {
        int lanceSize = int.Parse(lanceSetPair.Key);
        List<ExtendedLance> factions = lanceSetPair.Value;

        ExtendedLance lance = factions.FirstOrDefault(extendedLance => extendedLance.Faction == factionKey);

        if (lance != null) {
          return lanceOverride.lanceDifficultyAdjustment + lance.DifficultyMod;
        }
      }

      return lanceOverride.lanceDifficultyAdjustment;
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

      Main.Logger.Log($"[ExtendedLances.GetValidContractTypes] Valid contracts are '{string.Join(", ", validContracts.ToArray())}'");
      return validContracts;
    }
  }
}