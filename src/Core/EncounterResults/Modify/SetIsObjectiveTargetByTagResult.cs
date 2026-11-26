using UnityEngine;

using System;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Framework;
using BattleTech.UI;

using HBS.Collections;

namespace MissionControl.Result {
  public class SetIsObjectiveTargetByTagResult : EncounterResult {
    public bool IsObjectiveTarget { get; set; } = true;
    public string[] Tags { get; set; }

    public override void Trigger(MessageCenterMessage inMessage, string triggeringName) {
      if (Tags == null || Tags.Length == 0) {
        Main.Logger.LogError("[SetIsObjectiveTargetByTagResult] No tags provided. Skipping.");
        return;
      }

      Main.LogDebug($"[SetIsObjectiveTargetByTagResult] Setting IsObjectiveTarget '{IsObjectiveTarget}' with tags '{String.Concat(Tags)}'");
      List<ICombatant> combatants = ObjectiveGameLogic.GetTaggedCombatants(UnityGameInstance.BattleTechGame.Combat, new TagSet(Tags));

      Main.LogDebug($"[SetIsObjectiveTargetByTagResult] Found '{combatants.Count}' combatants");

      GameObject hudPanel = GameObject.Find("uixPrfPanl_HUD(Clone)");
      CombatHUDInWorldElementMgr inworldElementManager = hudPanel?.GetComponent<CombatHUDInWorldElementMgr>();

      foreach (ICombatant combatant in combatants) {
        BattleTech.Building building = combatant as BattleTech.Building;
        if (building == null || building.IsDead) continue;

        Main.LogDebug($"[SetIsObjectiveTargetByTagResult] Found building '{building.GameRep?.name} - {building.DisplayName}'");
        ObstructionGameLogic obstructionGameLogic = building.GameRep?.GetComponent<ObstructionGameLogic>();
        if (obstructionGameLogic != null) {
          obstructionGameLogic.isObjectiveTarget = IsObjectiveTarget;
        }
        building.isObjectiveTarget = IsObjectiveTarget;

        if (IsObjectiveTarget && inworldElementManager != null) {
          inworldElementManager.AddTickMark(building);
          inworldElementManager.AddInWorldActorElements(building);
        }
      }
    }
  }
}
