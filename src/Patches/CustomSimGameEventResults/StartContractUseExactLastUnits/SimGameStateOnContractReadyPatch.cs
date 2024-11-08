using Harmony;

using BattleTech;
using System.Collections.Generic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameState), "OnContractReady")]
  public class SimGameStateOnContractReadyPatch {
    static void Prefix(SimGameState __instance, ref LanceConfiguration config) {
      if (MissionControl.Instance.IsNextContractUseExactLastUnits) {
        LanceConfiguration lastLance = __instance.GetLastLance();

        if (lastLance == null) return;

        if (lastLance != null) {
          Main.Logger.Log($"[SimGameStateOnContractReadyPatch Prefix] Overwriting LanceConfiguration with last lance since it's a 'IsNextContractUseExactLastUnits' contract");
          FilterDeadMechsAndPilots(__instance, lastLance);
          config = lastLance;
        }
      }
    }

    private static void FilterDeadMechsAndPilots(SimGameState simGameState, LanceConfiguration lastLance) {
      List<SpawnableUnit> units = lastLance.Lances[TeamUtils.PLAYER_TEAM_ID];
      WeightedList<Pilot> pilotRoster = UnityGameInstance.Instance.Game.Simulation.PilotRoster;
      List<SpawnableUnit> updatedUnits = new List<SpawnableUnit>();

      foreach (SpawnableUnit unit in units) {
        if (unit.Unit != null && !unit.Unit.IsDestroyed && unit.Pilot != null && !unit.Pilot.Incapacitated && unit.Pilot.DateOfDeath <= 0) {
          Main.Logger.Log($"[SimGameStateOnContractReadyPatch FilterDeadMechsAndPilots] Keeping unit and pilot for proceeding contract: {unit.Unit?.Name} / {unit.Pilot?.Description.Name}. Mech is damaged? '{unit.Unit.IsDamaged}'");
          updatedUnits.Add(unit);
        } else {
          Main.Logger.Log($"[SimGameStateOnContractReadyPatch FilterDeadMechsAndPilots] Removing dead unit or pilot from proceeding contract: {unit.Unit?.Name} / {unit.Pilot?.Description.Name}");
        }
      }

      lastLance.Lances[TeamUtils.PLAYER_TEAM_ID] = updatedUnits;
      simGameState.SaveLastLance(lastLance);
    }
  }
}