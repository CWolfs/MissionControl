using BattleTech;
using BattleTech.Framework;

namespace MissionControl.Conditional {
  public class LanceVisibleToPlayerConditional : DesignConditional {
    public string TargetLanceGuid { get; set; }

    public override bool Evaluate(MessageCenterMessage message, string responseName) {
      base.Evaluate(message, responseName);

      if (string.IsNullOrEmpty(TargetLanceGuid)) {
        Main.Logger.LogError($"[LanceVisibleToPlayerConditional] TargetLanceGuid is empty. '{responseName}'");
        return false;
      }

      VisibilityLevelIncreasedMessage visibilityMessage = message as VisibilityLevelIncreasedMessage;
      if (visibilityMessage == null) {
        return false;
      }

      CombatGameState combat = UnityGameInstance.BattleTechGame?.Combat;
      if (combat == null) {
        Main.Logger.LogError($"[LanceVisibleToPlayerConditional] Combat state is unavailable. '{responseName}'");
        return false;
      }

      VisibilityLevelAndAttribution visibility = visibilityMessage.VisibilityLevelAndAttribution;
      if (visibility.SpottingTeamGUID != combat.LocalPlayerTeamGuid) {
        return false;
      }

      if (visibility.VisibilityLevel <= VisibilityLevel.None) {
        return false;
      }

      AbstractActor targetActor = combat.FindActorByGUID(visibilityMessage.TargetUnitGUID);
      if (targetActor == null) {
        return false;
      }

      if (targetActor.lance == null || string.IsNullOrEmpty(targetActor.lance.GUID)) {
        return false;
      }

      if (targetActor.lance.GUID.Contains(TargetLanceGuid)) {
        Main.LogDebug($"[LanceVisibleToPlayerConditional] Player visibility matches target lance guid '{TargetLanceGuid}'. '{responseName}'");
        return true;
      }

      return false;
    }
  }
}
