using Harmony;

using BattleTech;

using MissionControl.Result;

namespace MissionControl.Patches {
  // When an actor switches teams mid-activation (e.g. SetTeamByLanceSpawnerGuid or SetTeamByTag
  // running while oldTeam is still executing an in-flight OrderSequence for that actor),
  // AbstractActor.OnActivationEnd calls OnTurnActorActivationComplete on actor.team (the new team).
  // That leaves oldTeam's TeamActivationSequence orphaned on StackManager.SequenceStack (soft-lock)
  // and logs "No team activation sequence found!" on the new team. Redirect completion back to the
  // team that started the activation.
  [HarmonyPatch(typeof(Team), "OnTurnActorActivationComplete")]
  public static class TeamOnTurnActorActivationCompletePatch {
    static bool Prefix(Team __instance, string actorGUID, bool isDeferringAction) {
      if (!TeamActivationRedirect.TryConsumeRedirect(actorGUID, __instance, out Team originalTeam)) {
        return true;
      }

      Main.LogDebug($"[TeamOnTurnActorActivationCompletePatch] Redirecting activation completion for actor '{actorGUID}' from '{__instance.Name}' back to '{originalTeam.Name}'.");

      AITeam originalAiTeam = originalTeam as AITeam;
      bool originalTeamIsActivating = originalTeam.ActivationSequence != null
        || originalTeam.IsActive
        || (originalAiTeam != null && originalAiTeam.runningInterruptPhase);

      if (originalTeamIsActivating) {
        originalTeam.OnTurnActorActivationComplete(actorGUID, isDeferringAction);
        TeamActivationRedirect.CompleteEmptyInterruptPhaseIfNeeded(originalAiTeam);
      } else {
        if (originalTeam.numUnitsEndingActivation > 0) {
          originalTeam.numUnitsEndingActivation--;
        }
        originalTeam.Combat.MessageCenter.PublishMessage(
          new TurnActorActivateCompleteMessage(originalTeam.CurrentRound, originalTeam.CurrentPhase, originalTeam.GUID, actorGUID, isDeferringAction));
      }

      return false;
    }
  }
}
