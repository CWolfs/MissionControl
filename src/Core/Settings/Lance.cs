using UnityEngine;

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
      DropWeightInfluenceSettings dropWeightSettings = Main.Settings.AdditionalLanceSettings.DropWeightInfluenceSettings;
      bool useElites = MissionControl.Instance.ShouldUseElites(faction, teamType);
      int lanceNumber = 0;

      float rawChanceToSpawn = this.ChanceToSpawn;
      if (useElites) {
        if (EliteLances.Overrides.ContainsKey("ChanceToSpawn")) {
          rawChanceToSpawn = float.Parse(EliteLances.Overrides["ChanceToSpawn"]);
        } else {
          Main.Logger.LogWarning($"[SelectNumberOfAdditionalLances] [{TeamType}] Elite lances should be selected but no elite 'ChanceToSpawn' provided. Falling back to defaults.");
        }
      }

      // Use a different calculation approach instead then. Not supporting skirmish with this feature.
      if (!MissionControl.Instance.IsSkirmish() && dropWeightSettings.Enable) {
        Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Using Drop Weight Settings");
        int contractDifficulty = MissionControl.Instance.CurrentContract.Override.finalDifficulty;
        int playerLanceDropDifficulty = MissionControl.Instance.PlayerLanceDropDifficultyValue;
        int difficultyDifference = playerLanceDropDifficulty - contractDifficulty;

        if (difficultyDifference > 0) { // Player is sending a stronger lance than the contract difficulty requires. Add more influence to AL enemy lances spawning.
          float spawnInfluenceModifier = difficultyDifference *
            ((TeamType == "Enemy") ? dropWeightSettings.EnemySpawnInfluencePerHalfSkullAbove : dropWeightSettings.AllySpawnInfluencePerHalfSkullAbove);

          rawChanceToSpawn = rawChanceToSpawn + spawnInfluenceModifier;
        } else if (difficultyDifference < 0) { // Player is sending a weaker lance than the contract difficulty requires. Add more influence to AL ally lances spawning.
          float spawnInfluenceModifier = difficultyDifference *
            ((TeamType == "Enemy") ? dropWeightSettings.EnemySpawnInfluencePerHalfSkullBelow : dropWeightSettings.AllySpawnInfluencePerHalfSkullBelow);

          rawChanceToSpawn = rawChanceToSpawn + spawnInfluenceModifier;
        }

        Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Drop weight raw chance to spawn: '{rawChanceToSpawn}'");
      }

      int resolvedChanceToSpawn = (int)(Mathf.Clamp(rawChanceToSpawn, 0, 1) * 100f);
      Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Resolved chance to spawn: '{resolvedChanceToSpawn}%'");

      int resolvedMax = Max;
      if (useElites) {
        if (EliteLances.Overrides.ContainsKey("Max")) {
          resolvedMax = int.Parse(EliteLances.Overrides["Max"]);
        } else {
          Main.Logger.LogWarning($"[SelectNumberOfAdditionalLances] [{TeamType}] Elite lances should be selected but no elite 'Max' provided. Falling back to defaults.");
        }
      }

      for (int i = 0; i < resolvedMax; i++) {
        int rollChance = UnityEngine.Random.Range(1, 100 + 1);
        if (rollChance > resolvedChanceToSpawn) break;
        lanceNumber++;
      }

      Main.Logger.Log($"[SelectNumberOfAdditionalLances] [{TeamType}] '{lanceNumber}' Additional Lance will be added, unless later overridden");
      return lanceNumber;
    }
  }
}