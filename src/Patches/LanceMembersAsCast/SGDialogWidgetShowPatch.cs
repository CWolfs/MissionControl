using Harmony;

using BattleTech.UI;
using BattleTech.StringInterpolation;

using BattleTech.UI.TMProWrapper;

using MissionControl.Interpolation;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SGDialogWidget), "Show")]
  public class SGDialogWidgetShowPatch {
    public static void Postfix(SGDialogWidget __instance, ConvDialogEntry ___currentEntry) {
      LocalizableText dialogText = (LocalizableText)AccessTools.Field(typeof(ConvDialogEntry), "dialogText").GetValue(___currentEntry);
      string text = dialogText.text;

      Main.LogDebug("[SGDialogWidgetShowPatch] Extracted text is: " + text);

      if (text == DialogueInterpolationConstants.SKIP_DIALOGUE) {
        __instance.ReceiveButtonPress("ContinueDialog");
      }
    }
  }
}