
using System.Collections.Generic;

using BattleTech;

using MissionControl.Data;

/**
This debug result will log out all stats for a scope if the key 'All' is used, or log out a specific stat
*/
namespace MissionControl.Result {
  public class DebugStatLoggerResult : EncounterResult {
    public Scope Scope { get; set; }
    public string Key { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[DebugStatLoggerResult] Triggering - Scope '{Scope}' Key '{Key}'");
      StatCollection stats = StatsUtils.GetStats(Scope.ToString());

      if (stats == null) {
        Main.Logger.LogWarning($"[DebugStatLoggerResult] Stat collection with scope '{Scope}' could not be found. Maybe there is no active flashpoint or no current system if it's trying to get that.");
      }

      if (Key == "All") {
        Dictionary<string, string> statValues = stats.GetStatKeyValues();
        Main.LogDebug($"[DebugStatLoggerResult][Scope: '{Scope}'] Stats size is: " + statValues.Count);

        foreach (var pair in statValues) {
          Main.LogDebug($"[DebugStatLoggerResult][Scope: '{Scope}'] Key: '{pair.Key} Value: '{pair.Value}'");
        }
      } else {
        if (stats.ContainsStatistic(Key)) {
          Statistic stat = stats.GetStatistic(Key);
          Main.LogDebug($"[DebugStatLoggerResult][Scope: '{Scope}'] Key: '{Key} Value: '{stat.CurrentValue.objVal.ToString()}'");
        } else {
          Main.Logger.LogWarning($"[DebugStatLoggerResult] Stat with key '{Key}' could not be found in scope '{Scope}'");
        }
      }
    }
  }
}