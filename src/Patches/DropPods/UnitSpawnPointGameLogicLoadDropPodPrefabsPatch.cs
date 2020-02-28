using UnityEngine;

using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "LoadDropPodPrefabs")]
  public class UnitSpawnPointGameLogicLoadDropPodPrefabsPatch {
    private static Vector3 offscreenDropPodPosition = new Vector3(10000f, 10000f, 10000f);

    static bool Prefix(UnitSpawnPointGameLogic __instance, ParticleSystem dropPodVfxPrefab, GameObject dropPodLandedPrefab) {
      Main.LogDebug("[UnitSpawnPointGameLogicLoadDropPodPrefabsPatch] Patching");
      ParticleSystem newdropPodVfxPrefab = null;

      if (dropPodVfxPrefab != null) {
        Main.LogDebug("[UnitSpawnPointGameLogicLoadDropPodPrefabsPatch] Building new dropPodVfxPrefab");
        newdropPodVfxPrefab = UnityEngine.Object.Instantiate<ParticleSystem>(dropPodVfxPrefab, __instance.transform);
        newdropPodVfxPrefab.transform.position = __instance.transform.position;
        newdropPodVfxPrefab.Pause();
        newdropPodVfxPrefab.Clear();
        AccessTools.Field(typeof(UnitSpawnPointGameLogic), "dropPodVfxPrefab").SetValue(__instance, newdropPodVfxPrefab);
      }

      if (dropPodLandedPrefab != null) {
        GameObject newDropPodLandedPrefab = UnityEngine.Object.Instantiate<GameObject>(dropPodLandedPrefab, offscreenDropPodPosition, Quaternion.identity);
        AccessTools.Field(typeof(UnitSpawnPointGameLogic), "dropPodLandedPrefab").SetValue(__instance, newDropPodLandedPrefab);
      }

      return false;
    }
  }
}