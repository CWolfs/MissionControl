using Harmony;

using BattleTech;

using MissionControl.LogicComponents.CombatStates;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Contract), "FinalizeKilledMechWarriors")]
  public class ContractFinalizeKilledMechwarriorsPatch {
    static bool Prefix(Contract __instance) {
      string shouldDisablePilotDeath = MissionControl.Instance.GetGameLogicData(DisablePilotDeathGameLogic.DISABLE_PILOT_DEATH);
      if (UnityGameInstance.BattleTechGame.Combat != null) {
        if (shouldDisablePilotDeath != null && shouldDisablePilotDeath == "true") {
          Main.LogDebug($"[ContractFinalizeKilledMechwarriorsPatch.Prefix] DisablePilotDeathGameLogic - Preventing pilot death.");

          foreach (UnitResult playerUnitResult in __instance.PlayerUnitResults) {
            Pilot pilot = playerUnitResult.pilot;
            PilotDef pilotDef = pilot.pilotDef;
            pilotDef?.SetRecentInjuryDamageType(DamageType.NOT_SET);
          }

          return false;
        }
      }
      return true;
    }

    static void Postfix(Pilot __instance) {
      MissionControl.Instance.RemoveGameLogicData(DisablePilotDeathGameLogic.DISABLE_PILOT_DEATH);
      MissionControl.Instance.RemoveGameLogicData(DisablePilotDeathGameLogic.DISABLE_PILOT_INJURY);
    }
  }
}