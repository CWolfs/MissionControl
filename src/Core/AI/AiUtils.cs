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
      if (unit.team.IsFriendly(UnityGameInstance.BattleTechGame.Combat.LocalPlayerTeam)) {
        return true;
      }
      return false;
    }
  }
}