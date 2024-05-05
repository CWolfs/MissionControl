using UnityEngine;

using Newtonsoft.Json;

namespace MissionControl.Data {
  public class PropDecalDef : IBundleItem {
    [JsonProperty("Key")]
    public string Key { get; set; }

    [JsonProperty("Material")]
    public PropMaterialDef Material { get; set; } = new PropMaterialDef("envMatStct_decals_generic");

    [JsonProperty("Position")]
    public VectorData Position { get; set; } = new VectorData(VectorData.VectorType.Local, Vector3.zero);

    [JsonProperty("Rotation")]
    public VectorData Rotation { get; set; } = new VectorData(VectorData.VectorType.Local, Vector3.zero);

    [JsonProperty("Scale")]
    public VectorData Scale { get; set; } = new VectorData(VectorData.VectorType.Local, new Vector3(1, 1, 1));

    [JsonProperty("SheetXCoordinate")]
    public int SheetXCoordinate { get; set; } = 0;

    [JsonProperty("SheetYCoordinate")]
    public int SheetYCoordinate { get; set; } = 0;

    [JsonProperty("Alpha")]
    public float Alpha { get; set; } = 1;

    [JsonProperty("Priority")]
    public int Priority { get; set; } = 100;

    public string BundlePath { get; set; }

    public PropDecalDef Clone() {
      PropDecalDef newPropDecalDef = new PropDecalDef();
      newPropDecalDef.Key = Key;
      newPropDecalDef.Material = Material;
      newPropDecalDef.Position = Position;
      newPropDecalDef.Rotation = Rotation;
      newPropDecalDef.Scale = Scale;
      newPropDecalDef.SheetXCoordinate = SheetXCoordinate;
      newPropDecalDef.SheetYCoordinate = SheetYCoordinate;
      newPropDecalDef.Alpha = Alpha;
      newPropDecalDef.Priority = Priority;
      newPropDecalDef.BundlePath = BundlePath;
      return newPropDecalDef;
    }
  }
}