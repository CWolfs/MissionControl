using System;

using Harmony;

using HBS.Logging;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Logger.LogImpl))]
  [HarmonyPatch("LogError")]
  [HarmonyPatch(new Type[] { typeof(object) })]
  public class LoggerLogErrorPatch {
    static bool Prefix(Logger __instance, object message) {
      if (message is string) {
        if (MissionControl.Instance.IsCustomContractType) {
          string exceptionMessage = (string)message;

          if (
            exceptionMessage.Contains("Unable to serialize EncounterLayerData") ||
            exceptionMessage.Contains("MissionControl.LogicComponents.Objectives.DestroyXDestructiblesObjective") ||
            exceptionMessage.Contains("MissionControl.Result.EncounterResultBox")
          ) {
            // Main.Logger.Log("Skipping exception message that would otherwise say: " + exceptionMessage);
            return false;
          }
        }
      }

      return true;
    }
  }
}