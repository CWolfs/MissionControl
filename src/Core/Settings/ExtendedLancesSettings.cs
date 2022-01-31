using System.Linq;
using System.Collections.Generic;

using BattleTech.Framework;

using HBS.Collections;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class ExtendedLancesSettings : AdvancedSettings {
    [JsonProperty("EnableForTargetAlly")]
    public bool EnableForTargetAlly { get; set; } = false;

    [JsonProperty("EnableForEmployerAlly")]
    public bool EnableForEmployerAlly { get; set; } = false;

    [JsonProperty("EnableForHostileToAll")]
    public bool EnableForHostileToAll { get; set; } = false;

    [JsonProperty("EnableForNeutralToAll")]
    public bool EnableForNeutralToAll { get; set; } = false;

    [JsonProperty("Autofill")]
    public bool Autofill { get; set; } = true;

    [JsonProperty("AutofillType")]
    public string AutofillType { get; set; } = "RespectEmpty";  // RespectEmpty, FillEmpty

    [JsonProperty("AutofillUnitCopyType")]
    public string AutofillUnitCopyType { get; set; } = "RandomInLance";  // FirstInLance, RandomInLance

    [JsonProperty("LanceSizes")]
    public Dictionary<string, List<ExtendedLance>> LanceSizes { get; set; } = new Dictionary<string, List<ExtendedLance>>();

    [JsonProperty("SkipWhenTaggedWithAny")]
    public List<string> SkipWhenTaggedWithAny { get; set; } = new List<string>();

    [JsonProperty("SkipWhenTaggedWithAll")]
    public List<string> SkipWhenTaggedWithAll { get; set; } = new List<string>();

    [JsonProperty("SkipWhenExcludeTagsContain")]
    public List<string> SkipWhenExcludeTagsContain { get; set; } = new List<string>();

    [JsonProperty("ForceLanceOverrideSizeWithTag")]
    public string ForceLanceOverrideSizeWithTag { get; set; } = "mc_force_extended_lance";

    [JsonProperty("ForceLanceDefSizeWithTag")]
    public string ForceLanceDefSizeWithTag { get; set; } = "mc_force_extended_lance";

    public TagSet GetSkipWhenTaggedWithAny() {
      return new TagSet(SkipWhenTaggedWithAny);
    }

    public TagSet GetSkipWhenTaggedWithAll() {
      return new TagSet(SkipWhenTaggedWithAll);
    }

    public TagSet GetSkipWhenExcludeTagsContain() {
      return new TagSet(SkipWhenExcludeTagsContain);
    }

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
  }
}