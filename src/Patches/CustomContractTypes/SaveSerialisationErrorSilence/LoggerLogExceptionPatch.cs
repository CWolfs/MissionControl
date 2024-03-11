using System;

using Harmony;

using HBS.Logging;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Logger.LogImpl))]
  [HarmonyPatch("LogException")]
  [HarmonyPatch(new Type[] { typeof(object), typeof(Exception) })]
  public class LoggerLogExceptionPatch {
    static bool Prefix(Logger __instance, object message, Exception exception) {
      if (message is string) {
        if (MissionControl.Instance.IsCustomContractType) {
          string exceptionMessage = (string)message;
          if (
            exceptionMessage.Contains("Unable to serialize EncounterLayerData") ||
            exceptionMessage.Contains("Unable to serialize AIOrderList") ||
            exceptionMessage.Contains("EncounterResultBox") ||
            exceptionMessage.Contains("DesignResultList") ||
            exceptionMessage.Contains("DestroyXDestructiblesObjective") ||
            exceptionMessage.Contains("EmptyCustomChunkGameLogic") ||
            exceptionMessage.Contains("EncounterAIOrderBox")
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