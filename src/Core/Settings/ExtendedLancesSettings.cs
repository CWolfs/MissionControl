using System.Linq;
using System.Collections.Generic;

using BattleTech.Framework;

using HBS.Collections;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class ExtendedLancesSettings : AdvancedSettings {
    [JsonProperty("Mode")]
    public string Mode { get; set; } = "Deep";  // Shallow, Deep

    [JsonProperty("Autofill")]
    public bool Autofill { get; set; } = true;

    [JsonProperty("LanceSizes")]
    public Dictionary<string, List<ExtendedLance>> LanceSizes { get; set; } = new Dictionary<string, List<ExtendedLance>>();

    [JsonProperty("SkipWhenTaggedWithAny")]
    public List<string> SkipWhenTaggedWithAny { get; set; } = new List<string>();

    [JsonProperty("SkipWhenTaggedWithAll")]
    public List<string> SkipWhenTaggedWithAll { get; set; } = new List<string>();

    [JsonProperty("SkipWhenExcludeTagsContain")]
    public List<string> SkipWhenExcludeTagsContain { get; set; } = new List<string>();

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