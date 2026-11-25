using Harmony;

using BattleTech;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;

using MissionControl.SaveSupport;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(GameInstanceSave))]
  [HarmonyPatch(MethodType.Constructor)]
  [HarmonyPatch(new System.Type[] { typeof(GameInstance), typeof(SaveReason) })]
  public class GameInstanceSaveConstructorPatch {
    static void Prefix(GameInstance gameInstance) {
      if (gameInstance.Combat != null && gameInstance.Simulation != null) {
        CombatStatePersistence.Store(gameInstance.Simulation);
      }
    }
  }
}
