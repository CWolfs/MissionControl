using System.Text;

using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  // MC-711: Diagnostic prefix for Team.CompleteActivation.
  // Fires only when the guard below matches the same condition that makes the game log
  // "No team activation sequence found!" (Team.cs:1660) - so steady-state logging volume is zero.
  // When it does fire, dumps every team's activation state + the actor/turn-director snapshot
  // so the intermittent Blackout turret soft-lock (issue #711) is diagnosable from a single log.
  [HarmonyPatch(typeof(Team), "CompleteActivation")]
  public class TeamCompleteActivationPatch {
    static void Prefix(Team __instance, string actorGUID, bool isDeferringAction) {
      if (__instance.ActivationSequence != null) return;

      CombatGameState combat = __instance.Combat;
      TurnDirector turnDirector = combat.TurnDirector;

      StringBuilder sb = new StringBuilder();
      sb.AppendLine("[TeamCompleteActivationPatch][MC-711] ActivationSequence is null - engine will log 'No team activation sequence found'.");
      sb.AppendLine($"  Team: name='{__instance.Name}' guid={__instance.GUID} isActive={__instance.IsActive} unitsCount={__instance.units.Count}");
      sb.AppendLine($"  Input: actorGUID={actorGUID} isDeferringAction={isDeferringAction}");

      AbstractActor actor = combat.ItemRegistry.GetItemByGUID<AbstractActor>(actorGUID);
      if (actor != null) {
        sb.AppendLine($"  Actor: {actor.LogDisplayName} team='{actor.team?.Name}' teamGuid={actor.team?.GUID} isInterruptActor={actor.IsInterruptActor} hasBegunActivation={actor.HasBegunActivation} hasActivatedThisRound={actor.HasActivatedThisRound} initiative={actor.Initiative}");
      } else {
        sb.AppendLine($"  Actor: <not found in ItemRegistry for guid {actorGUID}>");
      }

      sb.AppendLine($"  TurnDirector: currentRound={turnDirector.CurrentRound} currentPhase={turnDirector.CurrentPhase} activeTurnActorGuid={turnDirector.ActiveTurnActor?.GUID ?? "<null>"}");

      sb.AppendLine("  All teams snapshot:");
      foreach (Team team in combat.Teams) {
        sb.AppendLine($"    name='{team.Name}' guid={team.GUID} isActive={team.IsActive} activationSequenceSet={team.ActivationSequence != null} unitsCount={team.units.Count}");
      }

      Main.Logger.LogWarning(sb.ToString());
    }
  }
}
