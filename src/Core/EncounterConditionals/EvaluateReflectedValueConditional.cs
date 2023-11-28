using System;
using System.Reflection;

using BattleTech.Framework;

using Harmony;

namespace MissionControl.Conditional {
  public class EvaluateReflectedValueConditional : DesignConditional {
    public string FieldToCheck { get; set; }
    public string ValueOfFieldToCheckEquality { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);

      Main.LogDebug("[EvaluateReflectedValueConditional] Evaluating");
      if (message == null) {
        Main.LogDebug("[EvaluateReflectedValueConditional] Message is null");
        return false;
      }

      Type messageType = message.GetType();
      Main.LogDebug($"[EvaluateReflectedValueConditional] Attempting to evaluate '{FieldToCheck}' in '{messageType.Name}'");

      // First, attempt to find and evaluate a field
      FieldInfo fieldInfo = AccessTools.Field(messageType, FieldToCheck);
      if (fieldInfo != null) {
        return EvaluateMember(fieldInfo.GetValue(message));
      }

      // If a field wasn't found, attempt to find and evaluate a property
      PropertyInfo propertyInfo = AccessTools.Property(messageType, FieldToCheck);
      if (propertyInfo != null) {
        return EvaluateMember(propertyInfo.GetValue(message));
      }

      // If neither a field nor a property was found, log an error
      Main.Logger.LogError($"[EvaluateReflectedValueConditional] Could not find field or property '{FieldToCheck}' in '{messageType.Name}'");
      return false;
    }

    private bool EvaluateMember(object value) {
      string valueAsString = ConvertToString(value);
      if (valueAsString == ValueOfFieldToCheckEquality || valueAsString.ToLower() == ValueOfFieldToCheckEquality.ToLower()) {
        Main.LogDebug($"[EvaluateReflectedValueConditional] Matched field '{FieldToCheck}' value of '{valueAsString}'  to modder requested check value of '{ValueOfFieldToCheckEquality}'");
        return true;
      } else {
        Main.LogDebug($"[EvaluateReflectedValueConditional] There was no match between the field '{FieldToCheck}' value of '{valueAsString}' to modder requested check value of '{ValueOfFieldToCheckEquality}'");
        return false;
      }
    }

    private string ConvertToString(object value) {
      // Handle nulls separately
      if (value == null) return null;
      // Convert value to string for comparison, handle other types if needed
      return value.ToString();
    }
  }
}
