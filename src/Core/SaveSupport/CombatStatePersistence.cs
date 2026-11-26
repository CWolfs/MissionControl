using System;
using System.Collections.Generic;

using BattleTech;

using MissionControl.Result;

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
      public List<DelaySaveState> Delays { get; set; } = new List<DelaySaveState>();
    }

    public static void Store(SimGameState simGame) {
      if (simGame == null) return;

      SavedCombatState state = new SavedCombatState();
      state.EncounterTags.AddRange(MissionControl.Instance.EncounterTags);
      state.GameLogicData = MissionControl.Instance.GetAllGameLogicData();

      List<DelayResult> delayResults = MissionControl.Instance.DelayResults;
      for (int i = 0; i < delayResults.Count; i++) {
        DelaySaveState delayState = delayResults[i].GetSaveState();
        if (delayState == null) continue;
        delayState.Id = GetDelayId(delayResults[i], i);
        state.Delays.Add(delayState);
      }

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
      SavedCombatState state = Load(simGame);
      if (state == null) return;
      string json = simGame.CompanyStats.GetValue<string>(STAT_NAME);

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

    public static void RestoreDelays(SimGameState simGame) {
      SavedCombatState state = Load(simGame);
      if (state == null) return;

      Dictionary<string, DelayResult> delayResults = new Dictionary<string, DelayResult>();
      for (int i = 0; i < MissionControl.Instance.DelayResults.Count; i++) {
        delayResults[GetDelayId(MissionControl.Instance.DelayResults[i], i)] = MissionControl.Instance.DelayResults[i];
      }

      foreach (DelaySaveState delayState in state.Delays) {
        if (delayResults.TryGetValue(delayState.Id, out DelayResult delayResult)) {
          delayResult.RestoreSaveState(delayState);
        } else {
          Main.Logger.LogWarning($"[CombatStatePersistence.RestoreDelays] Couldn't find Delay '{delayState.Id}' so it won't be resumed");
        }
      }
    }

    private static SavedCombatState Load(SimGameState simGame) {
      if (simGame == null || !simGame.CompanyStats.ContainsStatistic(STAT_NAME)) return null;

      string json = simGame.CompanyStats.GetValue<string>(STAT_NAME);
      try {
        return JsonConvert.DeserializeObject<SavedCombatState>(json);
      } catch (Exception e) {
        Main.Logger.LogError($"[CombatStatePersistence.Load] Couldn't read saved combat state '{json}': {e.Message}");
        return null;
      }
    }

    private static string GetDelayId(DelayResult delayResult, int index) {
      return $"{index}:{delayResult.Name}";
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
