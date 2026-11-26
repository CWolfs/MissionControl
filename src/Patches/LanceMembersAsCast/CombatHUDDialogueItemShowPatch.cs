using Harmony;

using BattleTech;
using BattleTech.UI;
using BattleTech.StringInterpolation;

using MissionControl.Interpolation;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(CombatHUDDialogItem), "Show")]
  public class CombatHUDDialogueItemShowPatch {
    public static void Prefix(CombatHUDDialogItem __instance, ref string dialogText) {
      if (AsyncDialogSequencePlayPatch.IsHandlingAsyncDialogue || string.IsNullOrEmpty(dialogText)) return;

      dialogText = DialogueInterpolator.Instance.Interpolate(DialogueInterpolator.InterpolateType.PreInterpolate, dialogText, null);
      GameContext gameContext = UnityGameInstance.BattleTechGame.GetActiveContext();
      dialogText = Interpolator.Interpolate(dialogText, gameContext);
    }
  }
}