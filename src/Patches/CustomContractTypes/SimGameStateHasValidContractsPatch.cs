using Harmony;

using BattleTech;
using BattleTech.Framework;

using System.Collections.Generic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameState), "HasValidContracts")]
  public class SimGameStateHasValidContractsPatch {
    static void Postfix(SimGameState __instance, Dictionary<int, List<ContractOverride>> potentialContracts) {
      MissionControl.Instance.PotentialContracts = potentialContracts;
    }
  }
}