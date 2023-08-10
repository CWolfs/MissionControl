using BattleTech;
using BattleTech.Framework;

using MissionControl.Data;

namespace MissionControl.Conditional {
  public class EvaluateStatConditional : DesignConditional {
    public string Scope { get; set; }
    public string Key { get; set; }
    public DataType DataType { get; set; }
    public EvaluateOperation Operation { get; set; }
    public string Value { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);

      Main.LogDebug($"[EvaluateStatConditional] Evaluating Scope '{Scope}' Key '{Key}' DataType '{DataType}' Operation '{Operation}' Value '{Value}'");
      StatCollection stats = StatsUtils.GetStats(Scope);
      if (stats.ContainsStatistic(Key)) {
        switch (DataType) {
          case DataType.String: return EvaluateStringStat(stats);
          case DataType.Boolean: return EvaluateBooleanStat(stats);
          case DataType.Integer: return EvaluateIntegerStat(stats);
          case DataType.Float: return EvaluateFloatStat(stats);
        }

      } else {
        Main.LogDebug($"[EvaluateStatConditional] No stat with key '{Key}' exists in the stat collection.");

        // Even if the stat doesn't exist we can still resolve equality 
        if (Operation == EvaluateOperation.Equals) {
          if (Value == "") return true;
        } else if (Operation == EvaluateOperation.NotEquals) {
          if (Value != "") return true;
        }
      }

      return false;
    }

    private bool EvaluateStringStat(StatCollection stats) {
      string statValue = stats.GetValue<string>(Key);
      Main.LogDebug($"[EvaluateStatConditional.EvaluateStringStat] Stat value from Key '{Key}' is '{statValue}'");
      string testValue = Value;
      Main.LogDebug($"[EvaluateStatConditional.EvaluateStringStat] Testing test value '{testValue}' against stat value '{statValue}'");

      switch (Operation) {
        case EvaluateOperation.Equals: return (statValue == testValue);
        case EvaluateOperation.NotEquals: return (statValue != testValue);
      }

      return false;
    }

    private bool EvaluateBooleanStat(StatCollection stats) {
      bool statValue = stats.GetValue<bool>(Key);
      bool testValue = false;
      bool success = bool.TryParse(Value, out testValue);

      if (!success) {
        Main.Logger.LogError($"[EvaluateStatConditional.EvaluateBooleanStat] The incoming Value '{Value}' is not a valid boolean.");
        return false;
      }

      Main.LogDebug($"[EvaluateStatConditional.EvaluateBooleanStat] Testing test value '{testValue}' against stat value '{statValue}'");

      switch (Operation) {
        case EvaluateOperation.Equals: return (statValue == testValue);
        case EvaluateOperation.NotEquals: return (statValue != testValue);
      }

      return false;
    }

    private bool EvaluateIntegerStat(StatCollection stats) {
      int statValue = stats.GetValue<int>(Key);
      int testValue = 0;
      bool success = int.TryParse(Value, out testValue);

      if (!success) {
        Main.Logger.LogError($"[EvaluateStatConditional.EvaluateIntegerStat] The incoming Value '{Value}' is not a valid integer.");
        return false;
      }

      Main.LogDebug($"[EvaluateStatConditional.EvaluateIntegerStat] Testing test value '{testValue}' against stat value '{statValue}'");

      switch (Operation) {
        case EvaluateOperation.LessThanOrEqualsTo: return (statValue <= testValue);
        case EvaluateOperation.GreaterThanOrEqualsTo: return (statValue >= testValue);
        case EvaluateOperation.Equals: return (statValue == testValue);
        case EvaluateOperation.NotEquals: return (statValue != testValue);
        case EvaluateOperation.LessThan: return (statValue < testValue);
        case EvaluateOperation.GreaterThan: return (statValue > testValue);
      }

      return false;
    }

    private bool EvaluateFloatStat(StatCollection stats) {
      float statValue = stats.GetValue<float>(Key);
      float testValue = 0;
      bool success = float.TryParse(Value, out testValue);

      if (!success) {
        Main.Logger.LogError($"[EvaluateStatConditional.EvaluateFloatStat] The incoming Value '{Value}' is not a valid float.");
        return false;
      }

      Main.LogDebug($"[EvaluateStatConditional.EvaluateFloatStat] Testing test value '{testValue}' against stat value '{statValue}'");

      switch (Operation) {
        case EvaluateOperation.LessThanOrEqualsTo: return (statValue <= testValue);
        case EvaluateOperation.GreaterThanOrEqualsTo: return (statValue >= testValue);
        case EvaluateOperation.Equals: return (statValue == testValue);
        case EvaluateOperation.NotEquals: return (statValue != testValue);
        case EvaluateOperation.LessThan: return (statValue < testValue);
        case EvaluateOperation.GreaterThan: return (statValue > testValue);
      }

      return false;
    }
  }
}