using BattleTech;

public static class MapEncounterLayerDataCellExtensions {
  public static void RemoveRegion(this MapEncounterLayerDataCell layerDataCell, RegionGameLogic regionGameLogic) {
    if (layerDataCell.regionGuidList == null) return;

    if (layerDataCell.regionGuidList.Contains(regionGameLogic.encounterObjectGuid)) {
      layerDataCell.regionGuidList.Remove(regionGameLogic.encounterObjectGuid);
    }
  }
}