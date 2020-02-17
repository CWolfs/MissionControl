using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;

using HBS.Collections;

using Harmony;

namespace MissionControl.Result {
  public class SetIsObjectiveTargetByTagResult : EncounterResult {
    public bool IsObjectiveTarget { get; set; } = true;
    public string[] Tags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      Main.LogDebug($"[SetIsObjectiveTargetByTagResult] Setting IsObjectiveTarget '{IsObjectiveTarget}' with tags '{String.Concat(Tags)}'");
      List<ICombatant> combatants = ObjectiveGameLogic.GetTaggedCombatants(UnityGameInstance.BattleTechGame.Combat, new TagSet(Tags));

      Main.LogDebug($"[SetIsObjectiveTargetByTagResult] Found '{combatants.Count}' combatants");

      foreach (ICombatant combatant in combatants) {
        BattleTech.Building building = combatant as BattleTech.Building;

        if (building != null) {
          Main.LogDebug($"[SetIsObjectiveTargetByTagResult] Found building '{building.GameRep.name} - {building.DisplayName}'");
          ObstructionGameLogic obstructionGameLogic = building.GameRep.GetComponent<ObstructionGameLogic>();
          obstructionGameLogic.isObjectiveTarget = true;
          AccessTools.Field(typeof(BattleTech.Building), "isObjectiveTarget").SetValue(combatant, true);
        }

        CombatHUDInWorldElementMgr inworldElementManager = GameObject.Find("uixPrfPanl_HUD(Clone)").GetComponent<CombatHUDInWorldElementMgr>();
        AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "AddTickMark").Invoke(inworldElementManager, new object[] { combatant });
        AccessTools.Method(typeof(CombatHUDInWorldElementMgr), "AddInWorldActorElements").Invoke(inworldElementManager, new object[] { combatant });
      }
    }
  }
}
