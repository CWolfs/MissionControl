using BattleTech;
using BattleTech.UI;

using Harmony;

using System.Collections.Generic;

public static class CombatHUDInWorldElementMgrExtensions {
  public static CombatHUDFloatieStackActor GetFloatieStackForCombatant(this CombatHUDInWorldElementMgr combatHUDInWorldElementMgr, ICombatant combatant) {
    List<CombatHUDFloatieStackActor> FloatieStacks = (List<CombatHUDFloatieStackActor>)AccessTools.Field(typeof(CombatHUDInWorldElementMgr), "FloatieStacks").GetValue(combatHUDInWorldElementMgr);
    return FloatieStacks.Find((CombatHUDFloatieStackActor x) => x.DisplayedCombatant == combatant);
  }
}