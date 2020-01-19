using Harmony;

using BattleTech.Framework;

/*
  Prevents pathfinder from triggering regions
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SmartTriggerResponse), "Evaluate")]
  public class SmartTriggerResponseEvalutePatch {
    public static bool Prefix(SmartTriggerResponse __instance) {
      if (MissionControl.Instance.AllowMissionControl() && !MissionControl.Instance.IsMCLoadingFinished) {
        Main.LogDebug("[SmartTriggerResponse.Evaluate] MC is running so this is a pathfinder check. Ignoring so not to trigger regions.");
        return false;
      }
      return true;
    }
  }
}