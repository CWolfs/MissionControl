using UnityEngine;

using System.Collections.Generic;

using BattleTech;

namespace MissionControl.AI {
  public class AiUtils {
    public static bool IsOnFirstAction(AbstractActor unit) {
      if (!unit.HasMovedThisRound && !unit.CanMoveAfterShooting) {
        return true;
      }
      return false;
    }

    public static bool IsOnSecondAction(AbstractActor unit) {
      return !IsOnFirstAction(unit);
    }

    public static bool HasFollowLanceTarget(AbstractActor unit) {
      if (unit == null) return false;
  
      BehaviorVariableValue value = AiManager.Instance.GetBehaviourVariableValue(unit, MoveToFollowLanceNode.FOLLOW_LANCE_TARGET_GUID_KEY);
      if (value != null) {
        string lanceGuid = value.StringVal;
        Lance lance = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID(lanceGuid) as Lance;
        Main.Logger.Log($"[HasFollowLanceTarget] Unit '{unit.DisplayName}' has a follow target lance of guid '{lanceGuid}' and name '{lance.DisplayName}'");
        return true;
      }

      return false;
    }

    public static AbstractActor GetClosestDetectedEnemy(AbstractActor focusedUnit, Lance allyLance) {
      List<AbstractActor> detectedEnemyUnits = new List<AbstractActor>();
      detectedEnemyUnits.AddRange(focusedUnit.lance.team.GetDetectedEnemyUnits());
      detectedEnemyUnits.AddRange(allyLance.team.GetDetectedEnemyUnits());

      float num = -1f;
      AbstractActor closestActor = null;

      for (int i = 0; i < detectedEnemyUnits.Count; i++) {
        AbstractActor actor = detectedEnemyUnits[i] as AbstractActor;
        float magnitude = (actor.CurrentPosition - focusedUnit.CurrentPosition).magnitude;
        if (num < 0f || magnitude < num) {
          num = magnitude;
          closestActor = actor;
        }
      }

      return closestActor;
    }
  }
}