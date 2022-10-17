using UnityEngine;

using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "GetSpawnPosition")]
  public class UnitSpawnPointGameLogicGetSpawnPositionPatch {
    static void Postfix(UnitSpawnPointGameLogic __instance, ref Vector3 __result, UnitType ___unitType) {
      if (MissionControl.Instance.IsCustomContractType && ___unitType == UnitType.Turret) {
        __result = __instance.Position;
      }
    }
  }
}