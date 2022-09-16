using Harmony;

using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Briefing), "Init")]
  public class BriefingInitPatch {
    public static void Prefix(Briefing __instance) {
      if (DataManager.Instance.IsCustomContractType(MissionControl.Instance.CurrentContractType)) {
        UiManager.Instance.CreateContractTypeCredits();
      }
    }
  }
}