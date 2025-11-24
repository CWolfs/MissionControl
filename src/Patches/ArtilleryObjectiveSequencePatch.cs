using UnityEngine;

using BattleTech;
using BattleTech.UI;

using Harmony;
using HBS;

using MissionControl.Result;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(ArtilleryObjectiveSequence), "PlayFX")]
  public class ArtilleryObjectiveSequence_PlayFX_Patch {
    public static bool Prefix(ArtilleryObjectiveSequence __instance, Vector3 targetPosition) {
      // Check if this is a ScalableArtilleryObjectiveSequence
      if (__instance is ScalableArtilleryObjectiveSequence scalable) {
        Main.LogDebug($"[ArtilleryObjectiveSequence_PlayFX_Patch] Intercepted PlayFX for ScalableArtilleryObjectiveSequence at {targetPosition}");
        scalable.PlayScaledFX(targetPosition);
        return false; // Skip original method
      }

      return true; // Run original method for normal ArtilleryObjectiveSequence
    }
  }

  // Force deselect units during artillery to prevent visual indicators from displaying
  [HarmonyPatch(typeof(ArtilleryObjectiveSequence), "OnUpdate")]
  public class ArtilleryObjectiveSequence_OnUpdate_Patch {
    private static bool hasLoggedSelection = false;
    private static bool hasLoggedNoSelection = false;

    public static void Postfix(ArtilleryObjectiveSequence __instance) {
      // Access CombatHUD through UIManager
      CombatHUD hud = LazySingletonBehavior<UIManager>.Instance?.GetFirstModule<CombatHUD>();
      if (hud?.SelectionHandler != null) {
        AbstractActor selectedActor = hud.SelectionHandler.SelectedActor;

        // Continuously clear any selection that might occur during artillery
        if (selectedActor != null) {
          hud.SelectionHandler.DeselectActor(selectedActor);
          if (!hasLoggedSelection) {
            Main.Logger.Log($"[ArtilleryObjectiveSequence_OnUpdate_Patch] Continuously clearing selection ('{selectedActor.DisplayName}') during artillery");
            hasLoggedSelection = true;
          }
        } else if (!hasLoggedNoSelection) {
          Main.Logger.Log("[ArtilleryObjectiveSequence_OnUpdate_Patch] No unit selected during artillery (good)");
          hasLoggedNoSelection = true;
        }
      }
    }
  }

  // TODO: Test this patch to see if it helps with selection issues
  // ===================================================================
  // PREVENT AUTO-SELECTION DURING ARTILLERY
  // ===================================================================
  /*
  // Stop RemoveCompletedItems from processing state changes during artillery
  [HarmonyPatch(typeof(CombatSelectionHandler), "RemoveCompletedItems")]
  public class CombatSelectionHandler_RemoveCompletedItems_Patch {
    public static bool Prefix(CombatSelectionHandler __instance) {
      // Block state management during artillery
      if (__instance.Combat?.StackManager?.TopSequence is ArtilleryObjectiveSequence) {
        return false; // Skip the entire method
      }
      return true;
    }
  }

  // Stop AutoSelectActor from re-selecting units during artillery
  [HarmonyPatch(typeof(CombatSelectionHandler), "AutoSelectActor")]
  public class CombatSelectionHandler_AutoSelectActor_Patch {
    public static bool Prefix(CombatSelectionHandler __instance) {
      // Block auto-selection during artillery
      if (__instance.Combat?.StackManager?.TopSequence is ArtilleryObjectiveSequence) {
        return false; // Skip auto-selection
      }
      return true;
    }
  }

  // Optional: Stop message handlers from triggering selection during artillery
  [HarmonyPatch(typeof(CombatSelectionHandler), "OnReadyToMove")]
  public class CombatSelectionHandler_OnReadyToMove_Patch {
    public static bool Prefix(CombatSelectionHandler __instance) {
      // Block OnReadyToMove during artillery
      if (__instance.Combat?.StackManager?.TopSequence is ArtilleryObjectiveSequence) {
        return false;
      }
      return true;
    }
  }
  */

}
