using System.Globalization;

using BattleTech;

using MissionControl.Data;

/**
This result will set, remove or modify a stat in a scope
*/
namespace MissionControl.Result {
  public class SetStatResult : EncounterResult {
    public Scope Scope { get; set; }
    public string Key { get; set; }
    public DataType DataType { get; set; }
    public StatOperation Operation { get; set; }
    public string Value { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[SetStatResult] Triggering for Scope '{Scope}' Key '{Key}' DataType '{DataType}' Operation '{Operation}' Value '{Value}'");
      StatCollection stats = StatsUtils.GetStats(Scope);
      if (stats == null) return;

      if (Operation == StatOperation.Remove) {
        stats.RemoveStatistic(Key);
        return;
      }

      switch (DataType) {
        case DataType.String: TriggerForStringStat(stats); break;
        case DataType.Boolean: TriggerForBooleanStat(stats); break;
        case DataType.Integer: TriggerForIntegerStat(stats); break;
        case DataType.Float: TriggerForFloatStat(stats); break;
      }
    }

    private void TriggerForStringStat(StatCollection stats) {
      string setValue = Value;

      switch (Operation) {
        case StatOperation.Set: Set(stats, Key, setValue); break;
      }
    }

    private void TriggerForBooleanStat(StatCollection stats) {
      bool setValue = false;
      bool success = bool.TryParse(Value, out setValue);

      if (!success) {
        Main.Logger.LogError($"[SetStatResult.TriggerForBooleanStat] The incoming Value '{Value}' is not a valid boolean.");
        return;
      }

      switch (Operation) {
        case StatOperation.Set: Set(stats, Key, setValue); break;
      }
    }

    private void TriggerForIntegerStat(StatCollection stats) {
      int setValue = 0;
      bool success = int.TryParse(Value, out setValue);

      if (!success) {
        Main.Logger.LogError($"[SetStatResult.TriggerForIntegerStat] The incoming Value '{Value}' is not a valid integer.");
        return;
      }

      switch (Operation) {
        case StatOperation.Set: Set(stats, Key, setValue); break;
        case StatOperation.Modify: stats.ModifyStat("", -1, Key, StatCollection.StatOperation.Int_Add, setValue); break;
      }
    }

    private void TriggerForFloatStat(StatCollection stats) {
      float setValue = 0;
      bool success = float.TryParse(Value, NumberStyles.Float, CultureInfo.InvariantCulture, out setValue);

      if (!success) {
        Main.Logger.LogError($"[SetStatResult.TriggerForFloatStat] The incoming Value '{Value}' is not a valid float.");
        return;
      }

      switch (Operation) {
        case StatOperation.Set: Set(stats, Key, setValue); break;
        case StatOperation.Modify: stats.ModifyStat("", -1, Key, StatCollection.StatOperation.Float_Add, setValue); break;
      }
    }

    private void Set<T>(StatCollection stats, string key, T value) {
      if (!stats.ContainsStatistic(key)) {
        stats.AddStatistic(key, value);
      } else {
        stats.ModifyStat("", -1, key, StatCollection.StatOperation.Set, value);
      }
    }
  }
}
