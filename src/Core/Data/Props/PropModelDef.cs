using UnityEngine;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;

namespace MissionControl.Data {
  public class PropModelDef {
    [JsonProperty("Key")]
    public string Key { get; set; }

    [JsonProperty("Mesh")]
    public string MeshName { get; set; }

    [JsonProperty("ChangePivotToCenterIfFlimsyMeshFormat")]
    public bool ChangePivotToCenterIfFlimsyMeshFormat { get; set; } = true;

    [JsonProperty("Materials")]
    public List<PropMaterialDef> Materials { get; set; } = new List<PropMaterialDef>();

    [JsonProperty("IsMeshInBundle")]
    public bool IsMeshInBundle { get; set; } = false;

    [JsonProperty("HasCustomSplits")]
    public bool HasCustomSplits { get; set; } = false;

    [JsonProperty("CustomSplitMaterials")]
    public List<PropMaterialDef> CustomSplitMaterials { get; set; } = new List<PropMaterialDef>();

    [JsonProperty("HasCustomShell")]
    public bool HasCustomShell { get; set; } = false;

    [JsonProperty("CustomShellMaterials")]
    public List<PropMaterialDef> CustomShellMaterials { get; set; } = new List<PropMaterialDef>();

    [JsonProperty("DestructibleSize")]
    private string destructibleSize { get; set; } = "large";

    [JsonProperty("MeshOffsets")]
    public Dictionary<string, Vector3> MeshOffsets = new Dictionary<string, Vector3>();

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

    public string BundlePath { get; set; }
  }
}