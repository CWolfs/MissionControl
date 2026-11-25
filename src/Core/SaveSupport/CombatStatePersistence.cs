using System;
using System.Collections.Generic;

using BattleTech;

using Newtonsoft.Json;

namespace MissionControl.SaveSupport {
  public static class CombatStatePersistence {
    public static string STAT_NAME = "MC_CombatState";

    private class SavedStat {
      public string Name { get; set; }
      public string Type { get; set; }
      public string Value { get; set; }
    }

    private class SavedCombatState {
      public List<string> EncounterTags { get; set; } = new List<string>();
      public List<SavedStat> EncounterStats { get; set; } = new List<SavedStat>();
      public Dictionary<string, string> GameLogicData { get; set; } = new Dictionary<string, string>();
    }

    public static void Store(SimGameState simGame) {
      if (simGame == null) return;

      SavedCombatState state = new SavedCombatState();
      state.EncounterTags.AddRange(MissionControl.Instance.EncounterTags);
      state.GameLogicData = MissionControl.Instance.GetAllGameLogicData();

      foreach (KeyValuePair<string, Statistic> stat in MissionControl.Instance.EncounterStats) {
        Type type = stat.Value.ValueType();
        string value = null;
        if (type == typeof(int)) value = stat.Value.Value<int>().ToString();
        else if (type == typeof(float)) value = stat.Value.Value<float>().ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        else if (type == typeof(bool)) value = stat.Value.Value<bool>().ToString();
        else if (type == typeof(string)) value = stat.Value.Value<string>();

        if (value == null) {
          Main.Logger.LogWarning($"[CombatStatePersistence.Store] Encounter stat '{stat.Key}' has unsupported type '{type}' so it won't survive a save");
          continue;
        }

        state.EncounterStats.Add(new SavedStat { Name = stat.Key, Type = type.Name, Value = value });
      }

      string json = JsonConvert.SerializeObject(state);
      if (simGame.CompanyStats.ContainsStatistic(STAT_NAME)) {
        simGame.CompanyStats.Set<string>(STAT_NAME, json);
      } else {
        simGame.CompanyStats.AddStatistic<string>(STAT_NAME, json);
      }
      Main.LogDebug($"[CombatStatePersistence.Store] Stored combat state: {json}");
    }

    public static void Restore(SimGameState simGame) {
      if (simGame == null || !simGame.CompanyStats.ContainsStatistic(STAT_NAME)) return;

      string json = simGame.CompanyStats.GetValue<string>(STAT_NAME);
      SavedCombatState state;
      try {
        state = JsonConvert.DeserializeObject<SavedCombatState>(json);
      } catch (Exception e) {
        Main.Logger.LogError($"[CombatStatePersistence.Restore] Couldn't read saved combat state '{json}': {e.Message}");
        return;
      }

      MissionControl.Instance.EncounterTags.AddRange(state.EncounterTags);
      foreach (KeyValuePair<string, string> data in state.GameLogicData) {
        MissionControl.Instance.SetGameLogicData(data.Key, data.Value);
      }

      StatCollection stats = MissionControl.Instance.EncounterStats;
      foreach (SavedStat stat in state.EncounterStats) {
        switch (stat.Type) {
          case "Int32": SetStat(stats, stat.Name, int.Parse(stat.Value)); break;
          case "Single": SetStat(stats, stat.Name, float.Parse(stat.Value, System.Globalization.CultureInfo.InvariantCulture)); break;
          case "Boolean": SetStat(stats, stat.Name, bool.Parse(stat.Value)); break;
          case "String": SetStat(stats, stat.Name, stat.Value); break;
          default: Main.Logger.LogWarning($"[CombatStatePersistence.Restore] Encounter stat '{stat.Name}' has unsupported type '{stat.Type}'"); break;
        }
      }
      Main.LogDebug($"[CombatStatePersistence.Restore] Restored combat state: {json}");
    }

    public static void Clear(SimGameState simGame) {
      if (simGame != null && simGame.CompanyStats.ContainsStatistic(STAT_NAME)) {
        simGame.CompanyStats.RemoveStatistic(STAT_NAME);
      }
    }

    private static void SetStat<T>(StatCollection stats, string name, T value) {
      if (stats.ContainsStatistic(name)) {
        stats.Set<T>(name, value);
      } else {
        stats.AddStatistic<T>(name, value);
      }
    }
  }
}
