using Harmony;

using BattleTech;
using BattleTech.UI;

using BattleTech.UI.TMProWrapper;

using MissionControl.Interpolation;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SGDialogWidget), "Show")]
  public class SGDialogWidgetShowPatch {
    public static void Prefix(SGDialogWidget __instance, ref CastDef whoIsTalking) {
      DialogueInterpolator.Instance.HandleDeadActorFromDialogueContent(ref whoIsTalking);
    }

    public static void Postfix(SGDialogWidget __instance, ConvDialogEntry ___currentEntry) {
      LocalizableText dialogText = (LocalizableText)AccessTools.Field(typeof(ConvDialogEntry), "dialogText").GetValue(___currentEntry);
      string text = dialogText.text;

      if (text == DialogueInterpolationConstants.SKIP_DIALOGUE) {
        __instance.ReceiveButtonPress("ContinueDialog");
      }
    }
  }
}