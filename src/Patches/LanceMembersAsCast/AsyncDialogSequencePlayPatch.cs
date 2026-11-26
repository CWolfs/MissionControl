using Harmony;

using BattleTech;
using BattleTech.StringInterpolation;

using MissionControl.Interpolation;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(AsyncDialogSequence), "Play")]
  public static class AsyncDialogSequencePlayPatch {
    public struct AsyncDialoguePlayState {
      public bool ShouldRestoreWords;
      public bool IncrementedDepth;
      public string OriginalWords;
    }

    private static int handlingAsyncDialogueDepth;
    public static bool IsHandlingAsyncDialogue => handlingAsyncDialogueDepth > 0;

    public static bool Prefix(AsyncDialogSequence __instance, DialogueContent msg, out AsyncDialoguePlayState __state) {
      __state = default;
      if (msg == null) return true;

      __state.OriginalWords = msg.words;
      __state.ShouldRestoreWords = true;

      if (UnityGameInstance.Instance.Game.Combat != null && msg.selectedCastDefId != CastDef.castDef_None) {
        CastDef speakerCastDef = msg.castDef;
        DialogueInterpolator.Instance.HandleDeadActorFromDialogueContent(ref speakerCastDef);
        msg.castDef = speakerCastDef;
      }

      if (msg.words == null) {
        SkipMessage(__instance, msg, ref __state);
        return false;
      }

      string text = DialogueInterpolator.Instance.Interpolate(DialogueInterpolator.InterpolateType.PreInterpolate, msg.words, msg.castDef);
      if (text == DialogueInterpolationConstants.SKIP_DIALOGUE) {
        SkipMessage(__instance, msg, ref __state);
        return false;
      }

      GameContext gameContext = UnityGameInstance.BattleTechGame.GetActiveContext();
      text = Interpolator.Interpolate(text, gameContext);
      if (text == DialogueInterpolationConstants.SKIP_DIALOGUE || (string.IsNullOrEmpty(text) && !msg.HasAudioEvent())) {
        SkipMessage(__instance, msg, ref __state);
        return false;
      }

      msg.words = text;

      handlingAsyncDialogueDepth++;
      __state.IncrementedDepth = true;
      return true;
    }

    public static void Postfix(DialogueContent msg, AsyncDialoguePlayState __state) {
      if (__state.IncrementedDepth && handlingAsyncDialogueDepth > 0) {
        handlingAsyncDialogueDepth--;
      }

      if (msg != null && __state.ShouldRestoreWords) {
        msg.words = __state.OriginalWords;
      }
    }

    private static void SkipMessage(AsyncDialogSequence sequence, DialogueContent msg, ref AsyncDialoguePlayState state) {
      msg.words = state.OriginalWords;
      state.ShouldRestoreWords = false;

      if (sequence.activeDialog != null) {
        sequence.activeDialog.afterShow = null;
        sequence.activeDialog.afterHide = null;
        sequence.activeDialog = null;
      }

      sequence.activeMessage = null;
      sequence.Combat.MessageCenter.PublishMessage(new DialogueContinueMessage(sequence.dialogueGuid, msg.conversationIdx));
      sequence.PlayNextMessage();
    }
  }
}
