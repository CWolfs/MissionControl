using System.Linq;
using System.Globalization;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace MissionControl.Config {
  public class AdditionalLances {
    [JsonProperty("IncludeContractTypes")]
    public List<string> IncludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("ExcludeContractTypes")]
    public List<string> ExcludeContractTypes { get; set; } = new List<string>();

    [JsonProperty("LancePool")]
    public Dictionary<string, List<string>> LancePool { get; set; } = new Dictionary<string, List<string>>() {
      { "ALL", new List<string>{"Generic_Light_Battle_Lance"} }
    };

    [JsonProperty(PropertyName = "RewardsPerLance", ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<Dictionary<string, string>> RewardsPerLance { get; set; } = new List<Dictionary<string, string>>() {
      new Dictionary<string, string>() {
        { "Type", "ContractBonusRewardPct" },
        { "Value", "0.75" }
      },
    };

    public string GetRewardType(Dictionary<string, string> reward) {
      if (reward.ContainsKey("Type")) {
        return reward["Type"];
      } else {
        Main.LogDebugWarning("[AdditionalLances] You are setting 'RewardPerLance' but not setting 'Type'. Fix this!");
        return "NONE";
      }
    }

    public float GetRewardValue(Dictionary<string, string> reward) {
      if (reward.ContainsKey("Value")) {
        return float.Parse(reward["Value"], CultureInfo.InvariantCulture);
      } else {
        Main.LogDebugWarning("[AdditionalLances] You are setting 'RewardPerLance' but not setting 'Type'. Fix this!");
        return 0;
      }
    }

    public Dictionary<string, float> GetRewards() {
      Dictionary<string, float> rewards = new Dictionary<string, float>();
      foreach (Dictionary<string, string> reward in RewardsPerLance) {
        string type = GetRewardType(reward);
        float value = GetRewardValue(reward);
        rewards.Add(type, value);
      }
      return rewards;
    }

    [JsonProperty("Player")]
    public Lance Player { get; set; } = new Lance();

    [JsonProperty("Enemy")]
    public Lance Enemy { get; set; } = new Lance("Enemy");

    [JsonProperty("Allies")]
    public Lance Allies { get; set; } = new Lance("Allies");

    public List<string> GetLancePoolKeys(string teamType, string biome, string contractType, string faction, int factionRep, int mrbLevel, int mrbRating) {
      List<string> lancePoolKeys = new List<string>();
      Dictionary<string, List<string>> teamLancePool = null;

      switch (teamType.ToLower()) {
        case "enemy":
          teamLancePool = Enemy.LancePool;
          break;
        case "allies":
          teamLancePool = Allies.LancePool;
          break;
      }

      lancePoolKeys.AddRange(GetLancePoolKeys(LancePool, teamType, biome, contractType, faction, factionRep, mrbLevel, mrbRating));
      if (teamLancePool != null) lancePoolKeys.AddRange(GetLancePoolKeys(teamLancePool, teamType, biome, contractType, faction, factionRep, mrbLevel, mrbRating));

      return lancePoolKeys.Distinct().ToList();
    }

    private List<string> GetLancePoolKeys(Dictionary<string, List<string>> lancePool, string teamType, string biome, string contractType, string faction, int factionRep, int mrbLevel, int mrbRep) {
      List<string> lancePoolKeys = new List<string>();
      string allIdentifier = "ALL";
      string biomeIdentifier = $"BIOME:{biome}";
      string contractTypeIdentifier = $"CONTRACT_TYPE:{contractType}";
      string factionIdentifier = $"FACTION:{faction}";
      string mrbLevelIdentifier = $"MRB_LEVEL:{mrbLevel}";
      string mrbRatingIdentifier = $"MRB_RATING";

      if (lancePool.ContainsKey(allIdentifier)) lancePoolKeys.AddRange(lancePool[allIdentifier]);
      if (lancePool.ContainsKey(biomeIdentifier)) lancePoolKeys.AddRange(lancePool[biomeIdentifier]);
      if (lancePool.ContainsKey(contractTypeIdentifier)) lancePoolKeys.AddRange(lancePool[contractTypeIdentifier]);

      Dictionary<string, List<string>> factionLances = lancePool.Where(lancePoolEntry => lancePoolEntry.Key.StartsWith(factionIdentifier)).ToDictionary(lancePoolEntry => lancePoolEntry.Key, lancePoolEntry => lancePoolEntry.Value);
      foreach (KeyValuePair<string, List<string>> factionLancesPair in factionLances) {
        string[] key = factionLancesPair.Key.Split(':');
        int minRep = int.Parse(key[2]);
        int maxRep = int.Parse(key[3]);
        if (factionRep >= minRep && factionRep <= maxRep) {
          lancePoolKeys.AddRange(factionLancesPair.Value);
          break;
        }
      }

      if (lancePool.ContainsKey(mrbLevelIdentifier)) lancePoolKeys.AddRange(lancePool[mrbLevelIdentifier]);

      Dictionary<string, List<string>> mrbLances = lancePool.Where(lancePoolEntry => lancePoolEntry.Key.StartsWith(mrbRatingIdentifier)).ToDictionary(lancePoolEntry => lancePoolEntry.Key, lancePoolEntry => lancePoolEntry.Value);
      foreach (KeyValuePair<string, List<string>> mrbLancesPair in mrbLances) {
        string[] key = mrbLancesPair.Key.Split(':');
        int minRep = int.Parse(key[1]);
        int maxRep = int.Parse(key[2]);
        if (mrbRep >= minRep && mrbRep <= maxRep) {
          lancePoolKeys.AddRange(mrbLancesPair.Value);
          break;
        }
      }

      return lancePoolKeys;
    }

    public List<string> GetValidContractTypes(string teamType) {
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

      if (teamType.ToLower() == "enemy") {
        if (Enemy.ExcludeContractTypes.Count > 0) validContracts = validContracts.Except(Enemy.ExcludeContractTypes).ToList();
      } else if (teamType.ToLower() == "allies") {
        if (Allies.ExcludeContractTypes.Count > 0) validContracts = validContracts.Except(Allies.ExcludeContractTypes).ToList();
      }

      Main.Logger.Log($"[AdditionalLances.GetValidContractTypes] Valid contracts are '{string.Join(", ", validContracts.ToArray())}'");
      return validContracts;
    }

    public Config.Lance GetActiveAdditionalLanceByTeamType(string teamType) {
      return (teamType == "enemy") ? Main.Settings.ActiveAdditionalLances.Enemy : Main.Settings.ActiveAdditionalLances.Allies;
    }
  }
}