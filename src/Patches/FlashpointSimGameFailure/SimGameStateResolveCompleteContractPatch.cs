using Harmony;

using HBS;
using BattleTech;
using BattleTech.UI;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(SimGameState), "ResolveCompleteContract")]
  public class SimGameStateResolveCompleteContractPatch {
    private static Contract cachedContract = null;

    static void Prefix(SimGameState __instance) {
        SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
        cachedContract = simGameState.CompletedContract;
    }

    static void Postfix(SimGameState __instance) {
        SimGameState simGameState = UnityGameInstance.BattleTechGame.Simulation;
        bool contractIsNotASuccess = cachedContract.State != Contract.ContractState.Complete;
        bool canIgnoreMissionResults = (bool)AccessTools.Method(typeof(SimGameState), "CanIgnoreMissionResults").Invoke(simGameState, new object[] { cachedContract.Override.OnContractFailureResults });
    
        // This causes a vanilla bug where the SimGame UI doesn't re-enable - so let's enable it now
        if (contractIsNotASuccess && canIgnoreMissionResults) {
            LazySingletonBehavior<UIManager>.Instance.ResetFader(UIManagerRootType.UIRoot);
            simGameState.SetSimRoomState(DropshipLocation.SHIP);
        }

        cachedContract = null;
    }
  }
}