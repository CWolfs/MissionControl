using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Harmony;

using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using BattleTech.Framework;

using MissionControl;
using MissionControl.Logic;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MapModule), "get_SelectedMap")]
  public class MapModuleSelectedMapPatch {
    static MapAndEncounters mapAndEncounter = null;

    static bool Prefix(MapModule __instance, ref MapAndEncounters __result) {
      if (UiManager.Instance.ClickedQuickSkirmish) {
        Main.Logger.Log($"[MapModuleSelectedMapPatch Prefix] Patching SelectedMap");
        if (mapAndEncounter == null) {
          using (MetadataDatabase metadataDatabase = new MetadataDatabase()) {
            List<MapAndEncounters> mapAndEncounters = metadataDatabase.GetReleasedMapsAndEncountersByContractType(new ContractType[] { ContractType.ArenaSkirmish });
            int index = UnityEngine.Random.Range(0, mapAndEncounters.Count);
            mapAndEncounter = mapAndEncounters[index];
          }
        }
        
        __result = mapAndEncounter;
        return false;
      }

      return true;
    }
  }
}