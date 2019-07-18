
using System;
using Harmony;

using BattleTech;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(MapMetaData), "GetCellAt")]
	[HarmonyPatch(new Type[] { typeof(Point) })]
  public class MapMetaDataGetCellAtPatch {
    private static MapTerrainDataCell InvalidCell = new MapTerrainDataCell {
			terrainSteepness = 1000f,
			terrainHeight = 2000f,
			MapEncounterLayerDataCell = new MapEncounterLayerDataCell {
				buildingList = null,
				regionGuidList = null
			}
		};

    static bool Prefix(MapMetaData __instance, ref MapTerrainDataCell __result, Point index, MapTerrainDataCell[,] ___mapTerrainDataCells) {
      try {
        __result = ___mapTerrainDataCells[index.Z, index.X];
      } catch (IndexOutOfRangeException) {
        __result = InvalidCell;
      }
			return false; 
    }
  }
}