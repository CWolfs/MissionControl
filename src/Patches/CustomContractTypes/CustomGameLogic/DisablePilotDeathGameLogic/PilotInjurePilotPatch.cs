using Harmony;

using BattleTech;

using MissionControl.EncounterNodes.CombatStates;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Pilot), "InjurePilot")]
  public class PilotInjurePilotPatch {
    static bool Prefix(Pilot __instance) {
      string shouldDisablePilotInjury = MissionControl.Instance.GetGameLogicData(DisablePilotDeathGameLogic.DISABLE_PILOT_INJURY);
      if (UnityGameInstance.BattleTechGame.Combat != null) {
        if (shouldDisablePilotInjury != null && shouldDisablePilotInjury == "true") {
          Main.LogDebug($"[PilotInjurePilotPatch.Prefix] DisablePilotDeathGameLogic - Ignoring injury for pilot '{__instance.Callsign}'");
          return false;
        }
      }
      return true;
    }
  }
}