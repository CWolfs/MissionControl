using BattleTech;

public static class EncounterLayerDataExtensions {
  public static MapEncounterLayerDataCell GetSafeCellAt(this EncounterLayerData layerData, int x, int z) {
    if (layerData.IsWithinBounds(x, z)) {
      return layerData.GetCellAt(x, z);
    }

    return null;
  }
}