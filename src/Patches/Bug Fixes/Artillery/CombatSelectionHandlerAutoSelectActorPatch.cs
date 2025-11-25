using BattleTech;
using BattleTech.UI;

using Harmony;

namespace MissionControl.Patches {
  // Prevents visual artifacts (blue circle, white overlay) during artillery sequences
  // by blocking the auto-selection system from selecting units while artillery is active.

  // [HarmonyPatch(typeof(CombatSelectionHandler), "RemoveCompletedItems")]
  // public static class CombatSelectionHandler_RemoveCompletedItems_Patch {
  //   public static bool Prefix(CombatSelectionHandler __instance) {
  //     return __instance.Combat?.StackManager?.TopSequence is not ArtilleryObjectiveSequence;
  //   }
  // }

  [HarmonyPatch(typeof(CombatSelectionHandler), "AutoSelectActor")]
  public static class CombatSelectionHandlerAutoSelectActorPatch {
    public static bool Prefix(CombatSelectionHandler __instance) {
      return __instance.Combat?.StackManager?.TopSequence is not ArtilleryObjectiveSequence;
    }
  }

  // [HarmonyPatch(typeof(CombatSelectionHandler), "OnReadyToMove")]
  // public static class CombatSelectionHandler_OnReadyToMove_Patch {
  //   public static bool Prefix(CombatSelectionHandler __instance) {
  //     return __instance.Combat?.StackManager?.TopSequence is not ArtilleryObjectiveSequence;
  //   }
  // }
}
