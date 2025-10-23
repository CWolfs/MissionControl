using UnityEngine;

using BattleTech;

using Harmony;

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
}
