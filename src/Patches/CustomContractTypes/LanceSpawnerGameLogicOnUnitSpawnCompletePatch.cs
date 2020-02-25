using Harmony;

using System;
using System.Collections.Generic;

using MissionControl.Messages;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(LanceSpawnerGameLogic), "OnUnitSpawnComplete")]
  public class LanceSpawnerGameLogicOnUnitSpawnCompletePatch {
    static void Prefix(LanceSpawnerGameLogic __instance) {
      Main.LogDebug($"[LanceSpawnerGameLogicOnUnitSpawnCompletePatch] Patching Prefix");
      if (HasLanceSpawnCompleted(__instance)) {
        LanceSpawnedMessage lanceSpawnedMessage = new LanceSpawnedMessage(__instance.encounterObjectGuid, __instance.LanceGuid);
        EncounterLayerParent.EnqueueLoadAwareMessage(lanceSpawnedMessage);
        /*
        // From the unit spawn message - might need a similar one for lance spawning to interrupt
        if (this.triggerInterruptPhaseOnSpawn) {
          abstractActor.IsInterruptActor = true;
          base.Combat.StackManager.InsertInterruptPhase(team.GUID, unitSpawnedMessage.messageIndex);
        }
        */
      }
    }

    private static bool HasLanceSpawnCompleted(LanceSpawnerGameLogic lanceSpawnerGameLogic) {
      UnitSpawnPointGameLogic[] unitSpawnPointGameLogicList = lanceSpawnerGameLogic.unitSpawnPointGameLogicList;
      for (int i = 0; i < unitSpawnPointGameLogicList.Length; i++) {
        if (unitSpawnPointGameLogicList[i].IsSpawning || unitSpawnPointGameLogicList[i].unitSpawnInProgress) {
          return false;
        }
      }
      return true;
    }
  }
}