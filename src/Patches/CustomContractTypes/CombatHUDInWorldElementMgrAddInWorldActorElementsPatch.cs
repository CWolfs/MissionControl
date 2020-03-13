using Harmony;

using BattleTech;
using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(CombatHUDInWorldElementMgr), "AddInWorldActorElements")]
  public class CombatHUDInWorldElementMgrAddInWorldActorElementsPatch {
    static bool Prefix(CombatHUDInWorldElementMgr __instance, ICombatant combatant) {
      if (MissionControl.Instance.IsCustomContractType && combatant.team.GUID == TeamUtils.NEUTRAL_TO_ALL_TEAM_ID) {
        Main.LogDebug($"[CombatHUDInWorldElementMgrAddInWorldActorElementsPatch.Prefix] Disabling InWorldActorElements for NeutralToAll - {combatant.DisplayName}");
        return false;
      }

      return true;
    }
  }
}