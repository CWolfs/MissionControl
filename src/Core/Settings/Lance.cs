using System.Collections.Generic;

using Newtonsoft.Json;

using BattleTech;

namespace MissionControl.Config {
  public class Lance {

    public string TeamType { get; set; } = "NONE";

    public Lance() { }

    public Lance(string teamType) {
      TeamType = teamType;
    }

    [JsonProperty("Max")]
    public int Max { get; set; } = 0;

    [JsonProperty("ExcludeContractTypes")]
    public List<string> ExcludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("ChanceToSpawn")]
    public float ChanceToSpawn { get; set; } = 0;

    [JsonProperty("EliteLances")]
    public EliteLances EliteLances = new EliteLances();

    [JsonProperty("LancePool")]
    public Dictionary<string, List<string>> LancePool { get; set; } = new Dictionary<string, List<string>>();

    public int SelectNumberOfAdditionalLances(FactionDef faction, string teamType) {
      bool useElites = MissionControl.Instance.ShouldUseElites(faction, teamType);
      int lanceNumber = 0;

      float rawChanceToSpawn = this.ChanceToSpawn;
      if (useElites) {
        if (EliteLances.Overrides.ContainsKey("ChanceToSpawn")) {
          rawChanceToSpawn = float.Parse(EliteLances.Overrides["ChanceToSpawn"]);
        } else {
          Main.Logger.LogWarning("[SelectNumberOfAdditionalLances] Elite lances should be selected but no elite 'ChanceToSpawn' provided. Falling back to defaults.");
        }
      }
      int resolvedChanceToSpawn = (int)(rawChanceToSpawn * 100f);

      int resolvedMax = Max;
      if (useElites) {
        if (EliteLances.Overrides.ContainsKey("Max")) {
          resolvedMax = int.Parse(EliteLances.Overrides["Max"]);
        } else {
          Main.Logger.LogWarning("[SelectNumberOfAdditionalLances] Elite lances should be selected but no elite 'Max' provided. Falling back to defaults.");
        }
      }

      for (int i = 0; i < resolvedMax; i++) {
        int rollChance = UnityEngine.Random.Range(1, 100 + 1);
        if (rollChance > resolvedChanceToSpawn) break;
        lanceNumber++;
      }

      Main.Logger.Log($"[SelectNumberOfAdditionalLances] {lanceNumber}");
      return lanceNumber;
    }
  }
}