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
  
      BehaviorVariableValue value = AiManager.Instance.GetBehaviourVariableValue(unit, "String_LanceGuid");
      if (value != null) {
        string lanceGuid = value.StringVal;
        Lance lance = UnityGameInstance.BattleTechGame.Combat.ItemRegistry.GetItemByGUID(lanceGuid) as Lance;
        Main.Logger.Log($"[HasFollowLanceTarget] Unit '{unit.DisplayName}' has a follow target lance of guid '{lanceGuid}' and name '{lance.DisplayName}'");
        return true;
      }

      return false;
    }
  }
}