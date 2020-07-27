using Harmony;

using BattleTech;
using BattleTech.UI;

using MissionControl.Logic;
using MissionControl.Messages;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "OnRetreatConfirmed")]
  public class CombatHUDRetreatEscMenuOnRetreatConfirmedPatch {
    [HarmonyBefore(new string[] { "us.frostraptor.DisorderlyWithdrawal" })]
    static bool Prefix(CombatHUDRetreatEscMenu __instance) {
      if (MissionControl.Instance.AllowMissionControl() && MissionControl.Instance.IsDynamicWithdrawAllowed()) {
        Main.LogDebug("[CombatHUDRetreatEscMenuOnRetreatConfirmedPatch] Sending Dynamic Withdraw Trigger Message");
        ChunkMessage triggerDynamicWithdrawMessage = new ChunkMessage(ChunkLogic.DYNAMIC_WITHDRAW_CHUNK_GUID);
        UnityGameInstance.Instance.Game.Combat.MessageCenter.PublishMessage(triggerDynamicWithdrawMessage);
        AccessTools.Method(typeof(CombatHUDRetreatEscMenu), "OnRetreatCancelled").Invoke(__instance, null);
        if (!Main.Settings.DynamicWithdraw.DisorderlyWithdrawalCompatibility) return false;
      }
      return true;
    }
  }
}