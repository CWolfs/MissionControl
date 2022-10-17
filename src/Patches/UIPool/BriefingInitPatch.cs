using Harmony;

using BattleTech;
using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Briefing), "Init")]
  public class BriefingInitPatch {
    public static void Prefix(Briefing __instance, LoadingSpinnerAndTip_Widget ___spinnerGameTip) {
      if (DataManager.Instance.IsCustomContractType(MissionControl.Instance.CurrentContractType)) {
        UnityGameInstance.Instance.StartCoroutine(UiManager.Instance.CreateContractTypeCredits(___spinnerGameTip.gameObject));
      }
    }
  }
}