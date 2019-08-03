using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;
using BattleTech.UI;
using BattleTech.Designed;
using BattleTech.Framework;

using MissionControl.Logic;
using MissionControl.Messages;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(CombatHUDRetreatEscMenu), "OnRetreatConfirmed")]
  public class CombatHUDRetreatEscMenuOnRetreatConfirmedPatch {
    [HarmonyBefore(new string[] { "us.frostraptor.DisorderlyWithdrawal" })]
    static bool Prefix(CombatHUDRetreatEscMenu __instance) {
      if (Main.Settings.DynamicWithdraw.Enable && Main.Settings.DynamicWithdraw.OnWithdrawButton) {
        Main.LogDebug("[CombatHUDRetreatEscMenuOnRetreatConfirmedPatch] Sending Dynamic Withdraw Trigger Message");
        ChunkMessage triggerDynamicWithdrawMessage = new ChunkMessage(ChunkLogic.DYNAMIC_WITHDRAW_CHUNK_GUID);
        UnityGameInstance.Instance.Game.Combat.MessageCenter.PublishMessage(triggerDynamicWithdrawMessage);
        return false;
      }
      return true;
    }
  }
}