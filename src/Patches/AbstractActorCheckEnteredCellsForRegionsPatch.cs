using Harmony;

using BattleTech;

/*
  Prevents pathfinder from triggering regions
*/
namespace MissionControl.Patches {
  [HarmonyPatch(typeof(AbstractActor), "CheckEnteredCellsForRegions")]
  public class AbstractActorCheckEnteredCellsForRegionsPatch {
    public static bool Prefix(Contract __instance) {
      if (!MissionControl.Instance.IsMCLoadingFinished) {
        Main.LogDebug("[CheckEnteredCellsForRegions] MC is running so this is a pathfinder check. Ignoring so not to trigger regions.");
        return false;
      }
      return true;
    }
  }
}