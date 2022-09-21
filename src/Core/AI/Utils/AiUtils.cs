using UnityEngine;

using System.Linq;
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
      string teamGuid = focusedUnit.lance.team.GUID;

      List<AbstractActor> detectedEnemyUnits = new List<AbstractActor>();
      detectedEnemyUnits.AddRange(focusedUnit.lance.team.GetDetectedEnemyUnits());
      detectedEnemyUnits.AddRange(allyLance.team.GetDetectedEnemyUnits());

      detectedEnemyUnits.RemoveAll(potentialEnemy => {
        if (!UnityGameInstance.BattleTechGame.Combat.HostilityMatrix.IsEnemy(teamGuid, potentialEnemy.team.GUID)) {
          // Main.Logger.Log($"[GetClosestDetectedEnemy] Removing potential enemy Unit '{potentialEnemy.DisplayName}' because it's not an enemy.");
          return true;
        }

        if (potentialEnemy.IsDead) {
          Main.Logger.LogDebug($"[GetClosestDetectedEnemy] Removing potential enemy Unit '{potentialEnemy.DisplayName}' because it is a dead unit.");
          return true;
        }

        return false;
      });

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

    public static AbstractActor GetClosestVisibleEnemy(AbstractActor focusedUnit, Lance allyLance) {
      string teamGuid = focusedUnit.lance.team.GUID;

      List<AbstractActor> visibleEnemyUnits = new List<AbstractActor>();
      visibleEnemyUnits.AddRange(focusedUnit.lance.team.GetVisibleEnemyUnits());
      visibleEnemyUnits.AddRange(allyLance.team.GetVisibleEnemyUnits());

      visibleEnemyUnits.RemoveAll(potentialEnemy => {
        if (!UnityGameInstance.BattleTechGame.Combat.HostilityMatrix.IsEnemy(teamGuid, potentialEnemy.team.GUID)) {
          // Main.Logger.LogDebug($"[GetClosestVisibleEnemy] Removing potential enemy Unit '{potentialEnemy.DisplayName}' because it's not an enemy.");
          return true;
        }

        if (potentialEnemy.IsDead) {
          Main.Logger.LogDebug($"[GetClosestVisibleEnemy] Removing potential enemy Unit '{potentialEnemy.DisplayName}' because it is a dead unit.");
          return true;
        }

        return false;
      });

      float num = -1f;
      AbstractActor closestActor = null;

      for (int i = 0; i < visibleEnemyUnits.Count; i++) {
        AbstractActor actor = visibleEnemyUnits[i] as AbstractActor;
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