using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;

using BattleTech;

namespace SpawnVariation {
  [HarmonyPatch(typeof(UnitSpawnPointGameLogic), "GetSpawnPosition")]
  public class UnitSpawnPointGameLogicPatch {
    static void Postfix(UnitSpawnPointGameLogic __instance, ref Vector3 __result) {
      __instance.Position = new Vector3(UnityEngine.Random.Range(-100.0f, 100.0f), __instance.Position.y, UnityEngine.Random.Range(-100.0f, 100.0f));
      __instance.UpdateHexPosition();
      Vector3 vector = __instance.hexPosition;
      vector.y = __instance.Combat.MapMetaData.GetLerpedHeightAt(vector);
      __result = vector;
    }
  }
}