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
        case StatOperation.Remove: stats.RemoveStatistic(Key); break;
      }
    }

    private void TriggerForBooleanStat(StatCollection stats) {
      bool statValue = stats.GetValue<bool>(Key);
      bool setValue = false;
      bool success = bool.TryParse(Value, out setValue);

      if (!success) {
        Main.Logger.LogError($"[SetStatResult.TriggerForBooleanStat] The incoming Value '{Value}' is not a valid boolean.");
      }

      switch (Operation) {
        case StatOperation.Set: Set(stats, Key, setValue); break;
        case StatOperation.Remove: stats.RemoveStatistic(Key); break;
      }
    }

    private bool TriggerForIntegerStat(StatCollection stats) {
      int statValue = stats.GetValue<int>(Key);
      int setValue = 0;
      bool success = int.TryParse(Value, out setValue);

      if (!success) {
        Main.Logger.LogError($"[SetStatResult.TriggerForIntegerStat] The incoming Value '{Value}' is not a valid integer.");
        return false;
      }

      switch (Operation) {
        case StatOperation.Set: Set(stats, Key, setValue); break;
        case StatOperation.Remove: stats.RemoveStatistic(Key); break;
        case StatOperation.Modify: stats.ModifyStat("", -1, Key, StatCollection.StatOperation.Int_Add, setValue); break;
      }

      return false;
    }

    private bool TriggerForFloatStat(StatCollection stats) {
      float statValue = stats.GetValue<float>(Key);
      float setValue = 0;
      bool success = float.TryParse(Value, out setValue);

      if (!success) {
        Main.Logger.LogError($"[SetStatResult.TriggerForFloatStat] The incoming Value '{Value}' is not a valid float.");
        return false;
      }

      switch (Operation) {
        case StatOperation.Set: Set(stats, Key, setValue); break;
        case StatOperation.Remove: stats.RemoveStatistic(Key); break;
        case StatOperation.Modify: stats.ModifyStat("", -1, Key, StatCollection.StatOperation.Float_Add, setValue); break;
      }

      return false;
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
