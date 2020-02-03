using Harmony;

using BattleTech;

using MissionControl.Logic;

/*
  This patch is at the right point to allow for requesting resources.
  This allows the system to load the resources ready for using them
  in the game scene.
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(Contract), "BeginRequestResources")]
  public class ContractBeginRequestResourcesPatch {
    static void Postfix(Contract __instance, bool generateUnits) {
      if (MissionControl.Instance.AllowMissionControl()) {
        if (generateUnits) {
          Main.Logger.Log($"[ContractBeginRequestResourcesPatch Postfix] Patching BeginRequestResources");
          RequestUnits();
          MissionControl.Instance.RunEncounterRules(SpawnLogic.LogicType.RESOURCE_REQUEST);
        }
      }
    }

    static void RequestUnits() {
      PathFinderManager.Instance.RequestPathFinderMech();
      PathFinderManager.Instance.RequestPathFinderVehicle();
    }
  }
}