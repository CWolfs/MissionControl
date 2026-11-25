using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(AITeam), "getInvocationForCurrentUnit")]
  public static class AITeamGetInvocationForCurrentUnitPatch {
    public static bool Prefix(AITeam __instance, AbstractActor ___currentUnit, ref InvocationMessage __result) {
      if (___currentUnit == null || ___currentUnit is Mech || !___currentUnit.IsShutDown) return true;

      Main.LogDebug($"[AITeamGetInvocationForCurrentUnitPatch] '{___currentUnit.DisplayName}' is shut down. Ending its activation.");
      __result = new ReserveActorInvocation(___currentUnit, ReserveActorAction.DONE, __instance.Combat.TurnDirector.CurrentRound);
      return false;
    }
  }
}
