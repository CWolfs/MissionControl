using Harmony;

using BattleTech;
using BattleTech.UI;

using MissionControl.Logic;
using MissionControl.EncounterSequences;

namespace MissionControl.Patches {
  /// <summary>
  /// Patches CombatHUD.OnTriggerDialog to intercept DialogueDecisionGameLogic
  /// and create InterruptDialogDecisionSequence instead of standard dialogue
  /// </summary>
  [HarmonyPatch(typeof(CombatHUD), "OnTriggerDialog")]
  public static class CombatHUDOnTriggerDialogPatch {
    static bool Prefix(CombatHUD __instance, MessageCenterMessage message) {
      TriggerDialog triggerDialog = message as TriggerDialog;
      if (triggerDialog == null) {
        return true; // Not a TriggerDialog message, let vanilla handle it
      }

      // Try to get the dialogue as DialogueDecisionGameLogic
      DialogueDecisionGameLogic decisionDialogueLogic = null;
      try {
        decisionDialogueLogic = __instance.Combat.ItemRegistry.GetItemByGUID<DialogueDecisionGameLogic>(triggerDialog.DialogID);
      } catch (System.Exception e) {
        Main.Logger.LogDebug($"[CombatHUDOnTriggerDialogPatch] Not a DialogueDecisionGameLogic: {e.Message}");
      }

      if (decisionDialogueLogic == null) {
        return true; // Not our type, let vanilla handle it
      }

      Main.Logger.Log($"[CombatHUDOnTriggerDialogPatch] Detected DialogueDecisionGameLogic: {triggerDialog.DialogID}");

      // Replicate vanilla validation: check showOnlyOnce
      if (decisionDialogueLogic.showOnlyOnce && decisionDialogueLogic.dialogueShownStatus == DialogueShownStatus.Shown) {
        Main.Logger.Log("[CombatHUDOnTriggerDialogPatch] Decision dialogue already shown (showOnlyOnce), skipping");
        __instance.Combat.MessageCenter.PublishMessage(new DialogComplete(triggerDialog.DialogID));
        return false; // Skip vanilla
      }

      // Replicate vanilla validation: check for empty content
      if (decisionDialogueLogic.conversationContent == null || decisionDialogueLogic.conversationContent.contents.Length == 0) {
        Main.Logger.LogWarning("[CombatHUDOnTriggerDialogPatch] Decision dialogue has no content, skipping");
        __instance.Combat.MessageCenter.PublishMessage(new DialogComplete(triggerDialog.DialogID));
        return false; // Skip vanilla
      }

      // Maintain message flow: publish DialogueStartMessage for mod compatibility
      __instance.Combat.MessageCenter.PublishMessage(new DialogueStartMessage(triggerDialog.DialogID));

      // Create and add our custom decision sequence to the stack
      InterruptDialogDecisionSequence decisionSequence = new InterruptDialogDecisionSequence(
        __instance.Combat,
        __instance.DialogWidget,
        decisionDialogueLogic
      );

      __instance.Combat.MessageCenter.PublishMessage(new AddSequenceToStackMessage(decisionSequence));

      return false; // Skip vanilla handling
    }
  }
}
