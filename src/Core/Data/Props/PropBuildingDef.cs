using Newtonsoft.Json;

using System;
using System.Collections.Generic;

namespace MissionControl.Data {
  public class PropBuildingDef {
    [JsonProperty("Key")]
    public string Key { get; set; }

    [JsonProperty("BuildingDefID")]
    public string BuildingDefID { get; set; }

    [JsonProperty("MainModel")]
    public string MainModelKey { get; set; }

    [JsonProperty("FlimsyModels")]
    public List<PropFlimsyDef> FlimsyModels { get; set; } = new List<PropFlimsyDef>();

    [JsonProperty("DestructibleSize")]
    private string destructibleSize { get; set; } = "large";

    [JsonIgnore]
    public DestructibleObject.DestructibleSize DestructibleSize {
      get {
        return (DestructibleObject.DestructibleSize)Enum.Parse(typeof(DestructibleObject.DestructibleSize), destructibleSize);
      }
    }

    [JsonProperty("DestructibleMaterial")]
    public string destructibleMaterial { get; set; } = "metal";

    [JsonIgnore]
    public DestructibleObject.DestructibleMaterial DestructibleMaterial {
      get {
        return (DestructibleObject.DestructibleMaterial)Enum.Parse(typeof(DestructibleObject.DestructibleMaterial), destructibleMaterial);
      }
    }

    [JsonProperty("FlimsyDestructibleType")]
    public string flimsyDestructibleType { get; set; } = "largeMetal";

    [JsonIgnore]
    public FlimsyDestructType FlimsyDestructibleType {
      get {
        return (FlimsyDestructType)Enum.Parse(typeof(FlimsyDestructType), flimsyDestructibleType);
      }
    }

    public PropModelDef GetPropModelDef() {
      if (DataManager.Instance.ModelDefs.ContainsKey(MainModelKey)) {
        return DataManager.Instance.ModelDefs[MainModelKey];
      }

      Main.Logger.LogError($"[PropBuildingDef.GetPropModelDef] No PropModelDef found for key '{MainModelKey}'. This should not happen.");
      return null;
    }
  }
}