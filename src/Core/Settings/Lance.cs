using UnityEngine;

using System;
using System.Collections.Generic;
using System.Globalization;

using Newtonsoft.Json;

using BattleTech;

namespace MissionControl.Config {
  public class Lance {

    public string TeamType { get; set; } = "NONE";

    public Lance() { }

    public Lance(string teamType) {
      TeamType = teamType;
    }

    [JsonProperty("Min")]
    public int Min { get; set; } = 0;

    [JsonProperty("Max")]
    public int Max { get; set; } = 0;

    [JsonProperty("ExcludeContractTypes")]
    public List<string> ExcludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("ChanceToSpawn")]
    public float ChanceToSpawn { get; set; } = 0;

    [JsonProperty("EliteLances")]
    public EliteLances EliteLances = new EliteLances();

    [JsonProperty("FactionOverrides")]
    public Dictionary<string, FactionAdditionalLanceLimit> FactionOverrides { get; set; } = new Dictionary<string, FactionAdditionalLanceLimit>();

    [JsonProperty("LanceSizeOverrides")]
    public Dictionary<string, FactionAdditionalLanceLimit> LanceSizeOverrides { get; set; } = new Dictionary<string, FactionAdditionalLanceLimit>();

    [JsonProperty("LancePool")]
    public Dictionary<string, List<string>> LancePool { get; set; } = new Dictionary<string, List<string>>();

    public int SelectNumberOfAdditionalLances(FactionDef faction, string teamType) {
      DropWeightInfluenceSettings dropWeightSettings = Main.Settings.AdditionalLanceSettings.DropWeightInfluenceSettings;
      bool useElites = MissionControl.Instance.ShouldUseElites(faction, teamType);

      float rawChanceToSpawn = this.ChanceToSpawn;
      if (useElites) {
        if (EliteLances.Overrides.ContainsKey("ChanceToSpawn")) {
          rawChanceToSpawn = float.Parse(EliteLances.Overrides["ChanceToSpawn"], CultureInfo.InvariantCulture);
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
        int absDifficultyDifference = Math.Abs(playerLanceDropDifficulty - contractDifficulty);

        if (difficultyDifference > 0) { // Player is sending a stronger lance than the contract difficulty requires. Add more influence to AL enemy lances spawning.
          float spawnInfluenceModifier = absDifficultyDifference *
            ((TeamType == "Enemy") ? dropWeightSettings.EnemySpawnInfluencePerHalfSkullAbove : dropWeightSettings.AllySpawnInfluencePerHalfSkullAbove);

          if (Math.Abs(spawnInfluenceModifier) > dropWeightSettings.GetSpawnInfluenceMax(TeamType)) {
            spawnInfluenceModifier = (TeamType == "Enemy") ? dropWeightSettings.GetSpawnInfluenceMax(TeamType) : dropWeightSettings.GetSpawnInfluenceMax(TeamType) * -1;
            Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Calculated '{TeamType}' drop weight influence is above SpawnInfluenceMax. Clamping to: '{spawnInfluenceModifier}'");
          }

          Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] SpawnInfluenceModifier is: '{spawnInfluenceModifier}'");

          rawChanceToSpawn = rawChanceToSpawn + spawnInfluenceModifier;
        } else if (difficultyDifference < 0) { // Player is sending a weaker lance than the contract difficulty requires. Add more influence to AL ally lances spawning.
          float spawnInfluenceModifier = absDifficultyDifference *
            ((TeamType == "Enemy") ? dropWeightSettings.EnemySpawnInfluencePerHalfSkullBelow : dropWeightSettings.AllySpawnInfluencePerHalfSkullBelow);

          if (Math.Abs(spawnInfluenceModifier) > dropWeightSettings.GetSpawnInfluenceMax(TeamType)) {
            spawnInfluenceModifier = (TeamType == "Enemy") ? dropWeightSettings.GetSpawnInfluenceMax(TeamType) * -1 : dropWeightSettings.GetSpawnInfluenceMax(TeamType);
            Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Calculated '{TeamType}' drop weight influence is above SpawnInfluenceMax. Clamping to: '{spawnInfluenceModifier}'");
          }

          Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] SpawnInfluenceModifier is: '{spawnInfluenceModifier}''");

          rawChanceToSpawn = rawChanceToSpawn + spawnInfluenceModifier;
        }

        Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Drop weight raw chance to spawn: '{rawChanceToSpawn}'");
      }

      int resolvedChanceToSpawn = (int)(Mathf.Clamp(rawChanceToSpawn, 0, 1) * 100f);
      Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Resolved chance to spawn: '{resolvedChanceToSpawn}%'");

      ResolveAdditionalLanceBounds(faction, teamType, useElites, out int resolvedMin, out int resolvedMax, out _);
      Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Resolved Additional Lance bounds: Min '{resolvedMin}', Max '{resolvedMax}'");

      int lanceNumber = resolvedMin;
      for (int i = resolvedMin; i < resolvedMax; i++) {
        int rollChance = UnityEngine.Random.Range(1, 100 + 1);
        if (rollChance > resolvedChanceToSpawn) break;
        lanceNumber++;
      }

      Main.Logger.Log($"[SelectNumberOfAdditionalLances] [{TeamType}] '{lanceNumber}' Additional Lance will be added, unless later overridden");
      return lanceNumber;
    }

    public int ApplyFactionOverrideToMatchedCount(FactionDef faction, string teamType, int matchedCount) {
      bool useElites = MissionControl.Instance.ShouldUseElites(faction, teamType);
      ResolveAdditionalLanceBounds(faction, teamType, useElites, out int resolvedMin, out int resolvedMax, out bool appliedFactionOverride);

      if (!appliedFactionOverride) {
        return matchedCount;
      }

      int adjusted = Mathf.Clamp(matchedCount, resolvedMin, resolvedMax);

      if (adjusted != matchedCount) {
        string factionKey = ResolveFactionKey(faction, teamType);
        Main.Logger.Log($"[ApplyFactionOverrideToMatchedCount] [{TeamType}] Clamped matched lance count for faction '{factionKey}' from '{matchedCount}' to '{adjusted}' (bounds Min '{resolvedMin}', Max '{resolvedMax}').");
      }

      return adjusted;
    }

    private void ResolveAdditionalLanceBounds(FactionDef faction, string teamType, bool useElites, out int resolvedMin, out int resolvedMax, out bool appliedFactionOverride) {
      resolvedMin = Math.Max(0, Min);
      resolvedMax = Math.Max(0, Max);
      appliedFactionOverride = false;

      if (useElites) {
        if (EliteLances.Overrides.ContainsKey("Max")) {
          if (int.TryParse(EliteLances.Overrides["Max"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int eliteMax)) {
            resolvedMax = Math.Max(0, eliteMax);
          } else {
            Main.Logger.LogWarning($"[SelectNumberOfAdditionalLances] [{TeamType}] Invalid elite 'Max' value '{EliteLances.Overrides["Max"]}'. Falling back to defaults.");
          }
        } else {
          Main.Logger.LogWarning($"[SelectNumberOfAdditionalLances] [{TeamType}] Elite lances should be selected but no elite 'Max' provided. Falling back to defaults.");
        }

        if (EliteLances.Overrides.ContainsKey("Min")) {
          if (int.TryParse(EliteLances.Overrides["Min"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int eliteMin)) {
            resolvedMin = Math.Max(0, eliteMin);
          } else {
            Main.Logger.LogWarning($"[SelectNumberOfAdditionalLances] [{TeamType}] Invalid elite 'Min' value '{EliteLances.Overrides["Min"]}'. Falling back to defaults.");
          }
        }
      }

      string factionKey = ResolveFactionKey(faction, teamType);
      bool allowGlobalOverrides = resolvedMax > 0;
      if (TryResolveActiveOverride(factionKey, teamType, allowGlobalOverrides, out FactionAdditionalLanceLimit limitOverride, out string overrideSource)) {
        int previousMin = resolvedMin;
        int previousMax = resolvedMax;

        if (limitOverride.Max.HasValue) {
          resolvedMax = limitOverride.Max.Value;
        }

        if (limitOverride.MaxMod.HasValue) {
          resolvedMax += limitOverride.MaxMod.Value;
        }

        if (limitOverride.Min.HasValue) {
          resolvedMin = limitOverride.Min.Value;
        }

        resolvedMax = Math.Max(0, resolvedMax);
        resolvedMin = Math.Max(0, resolvedMin);

        if (resolvedMin > resolvedMax) {
          Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Clamping {overrideSource} Min '{resolvedMin}' to resolved Max '{resolvedMax}' for '{factionKey}'.");
          resolvedMin = resolvedMax;
        }

        appliedFactionOverride = true;
        Main.Logger.Log($"[SelectNumberOfAdditionalLances] [{TeamType}] Applied {overrideSource} override for '{factionKey}': Min '{previousMin}' -> '{resolvedMin}', Max '{previousMax}' -> '{resolvedMax}'.");
        return;
      }

      if (resolvedMin > resolvedMax) {
        Main.LogDebug($"[SelectNumberOfAdditionalLances] [{TeamType}] Clamping Min '{resolvedMin}' to Max '{resolvedMax}'.");
        resolvedMin = resolvedMax;
      }
    }

    private string ResolveFactionKey(FactionDef faction, string teamType) {
      FactionValue factionValue = faction?.FactionValue;
      if (factionValue != null && !factionValue.IsInvalidUnset && !string.IsNullOrEmpty(factionValue.Name)) {
        return factionValue.Name;
      }
      return MissionControl.Instance.GetFactionKeyFromTeamType(teamType);
    }

    private bool TryGetPerDifficultyFactionOverride(string factionKey, out FactionAdditionalLanceLimit limit) {
      limit = null;
      if (string.IsNullOrEmpty(factionKey) || FactionOverrides == null || FactionOverrides.Count == 0) return false;

      foreach (KeyValuePair<string, FactionAdditionalLanceLimit> entry in FactionOverrides) {
        if (string.Equals(entry.Key, factionKey, StringComparison.OrdinalIgnoreCase)) {
          limit = entry.Value;
          return true;
        }
      }

      return false;
    }

    private bool TryGetPerDifficultyLanceSizeOverride(int lanceSize, out FactionAdditionalLanceLimit limit) {
      limit = null;
      if (LanceSizeOverrides == null || LanceSizeOverrides.Count == 0) return false;

      string lanceSizeKey = lanceSize.ToString(CultureInfo.InvariantCulture);
      foreach (KeyValuePair<string, FactionAdditionalLanceLimit> entry in LanceSizeOverrides) {
        if (string.Equals(entry.Key?.Trim(), lanceSizeKey, StringComparison.OrdinalIgnoreCase)) {
          limit = entry.Value;
          return true;
        }
      }

      return false;
    }

    private bool TryResolveActiveOverride(string factionKey, string teamType, bool allowGlobalOverrides, out FactionAdditionalLanceLimit limit, out string overrideSource) {
      limit = null;
      overrideSource = null;

      if (!string.IsNullOrEmpty(factionKey) && TryGetPerDifficultyFactionOverride(factionKey, out limit)) {
        overrideSource = "per-difficulty faction";
        if (limit == null || !limit.HasLimits) {
          Main.LogDebug($"[TryResolveActiveOverride] [{TeamType}] Per-difficulty faction override for '{factionKey}' is empty. Skipping lower-priority overrides.");
          limit = null;
          return false;
        }
      } else if (allowGlobalOverrides && !string.IsNullOrEmpty(factionKey) && Main.Settings.AdditionalLanceSettings.TryGetFactionOverride(factionKey, teamType, out limit)) {
        overrideSource = "global faction";
      } else {
        if (!allowGlobalOverrides && !string.IsNullOrEmpty(factionKey) && Main.Settings.AdditionalLanceSettings.TryGetFactionOverride(factionKey, teamType, out _)) {
          Main.LogDebug($"[TryResolveActiveOverride] [{TeamType}] Skipping global faction override for '{factionKey}' because Max is 0 for this difficulty profile.");
        }

        int effectiveLanceSize = MissionControl.Instance.GetEffectiveExtendedLanceSizeForTeam(teamType, factionKey);
        if (TryGetPerDifficultyLanceSizeOverride(effectiveLanceSize, out limit)) {
          overrideSource = $"per-difficulty lance size '{effectiveLanceSize}'";
          if (limit == null || !limit.HasLimits) {
            Main.LogDebug($"[TryResolveActiveOverride] [{TeamType}] Per-difficulty lance size override for '{effectiveLanceSize}' is empty. Skipping global lance size override.");
            limit = null;
            return false;
          }
        } else if (allowGlobalOverrides && Main.Settings.AdditionalLanceSettings.TryGetLanceSizeOverride(effectiveLanceSize, teamType, out limit)) {
          overrideSource = $"global lance size '{effectiveLanceSize}'";
        } else {
          if (!allowGlobalOverrides && Main.Settings.AdditionalLanceSettings.TryGetLanceSizeOverride(effectiveLanceSize, teamType, out _)) {
            Main.LogDebug($"[TryResolveActiveOverride] [{TeamType}] Skipping global lance size '{effectiveLanceSize}' override for '{factionKey}' because Max is 0 for this difficulty profile.");
          }
          return false;
        }
      }

      if (limit.OnlyWhenExtendedLancesActive == true && !MissionControl.Instance.IsExtendedLancesActiveForTeam(teamType, factionKey)) {
        Main.LogDebug($"[TryResolveActiveOverride] [{TeamType}] Skipping {overrideSource} override for '{factionKey}' because Extended Lances is not active for this team.");
        limit = null;
        return false;
      }

      if (limit.OnlyWhenLargerThanOpposingLance == true && !MissionControl.Instance.IsExtendedLanceLargerThanOpposingTeam(teamType, factionKey)) {
        Main.LogDebug($"[TryResolveActiveOverride] [{TeamType}] Skipping {overrideSource} override for '{factionKey}' because this team's lance size is not larger than the opposing team's lance size.");
        limit = null;
        return false;
      }

      if (limit.OnlyWhenLargerThanPlayerDrop == true && !MissionControl.Instance.IsExtendedLanceLargerThanPlayerDrop(teamType, factionKey)) {
        Main.LogDebug($"[TryResolveActiveOverride] [{TeamType}] Skipping {overrideSource} override for '{factionKey}' because this team's lance size is not larger than the player's drop unit count.");
        limit = null;
        return false;
      }

      return true;
    }
  }
}