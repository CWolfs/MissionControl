using Harmony;

using BattleTech;
using BattleTech.UI;
using BattleTech.StringInterpolation;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(CombatHUDDialogItem), "Show")]
  public class CombatHUDDialogueItemShowPatch {
    public static void Prefix(CombatHUDDialogItem __instance, ref string dialogText) {
      GameContext gameContext = UnityGameInstance.BattleTechGame.GetActiveContext();
      dialogText = Interpolator.Interpolate(dialogText, gameContext);
    }
  }
}