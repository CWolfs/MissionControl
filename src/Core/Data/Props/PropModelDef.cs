using UnityEngine;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;

using BTAssetReader.Core.Models;
using BTAssetReader.Unity;

namespace MissionControl.Data {
  public class PropModelDef : IBundleItem {
    [JsonProperty("Key")]
    public string Key { get; set; }

    [JsonProperty("IsVanilla")]
    public bool IsVanilla { get; set; } = false;

    [JsonProperty("AssetSource")]
    public string AssetSource { get; set; } = "Auto";

    [JsonProperty("BundleOverride")]
    public string BundleOverride { get; set; }

    [JsonProperty("Mesh")]
    public string MeshName { get; set; }

    [JsonProperty("SourceAssetFile")]
    public string SourceAssetFile { get; set; }

    [JsonProperty("MeshPathIds")]
    public Dictionary<string, long> MeshPathIds { get; set; } = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

    [JsonProperty("SplitPrefabName")]
    public string SplitPrefabName { get; set; }

    [JsonProperty("PivotMode")]
    public string PivotMode { get; set; }

    [JsonProperty("ChangePivotToCenterIfFlimsyMeshFormat")]
    public bool ChangePivotToCenterIfFlimsyMeshFormat { get; set; } = true;

    [JsonProperty("Materials")]
    public List<PropMaterialDef> Materials { get; set; } = new List<PropMaterialDef>();

    [JsonProperty("GlassMaterial")]
    public PropMaterialDef GlassMaterial { get; set; } = new PropMaterialDef("envMatStct_glassA_decals_generic");

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

    [JsonProperty("CompleteBundle")]
    public string CompleteBundleName { get; set; } = null;

    [JsonProperty("Scale")]
    public VectorData Scale { get; set; } = new VectorData(VectorData.VectorType.Local, Vector3.one);

    // Custom file model (.glb / .obj) settings
    [JsonProperty("ModelFile")]
    public string ModelFile { get; set; }

    [JsonProperty("RotationOffset")]
    public Vector3 RotationOffset { get; set; } = Vector3.zero;

    [JsonProperty("PositionOffset")]
    public Vector3 PositionOffset { get; set; } = Vector3.zero;

    [JsonProperty("TargetHeight")]
    public float TargetHeight { get; set; } = 0f;

    [JsonProperty("ColliderMode")]
    public string ColliderMode { get; set; } = "Mesh";

    [JsonProperty("SplitMode")]
    public string SplitMode { get; set; } = "Auto";

    [JsonProperty("FractureCount")]
    public int FractureCount { get; set; } = 8;

    [JsonProperty("FractureSeed")]
    public int FractureSeed { get; set; } = 0;

    [JsonProperty("ShellMode")]
    public string ShellMode { get; set; } = "Auto";

    [JsonProperty("StumpHeight")]
    public float StumpHeight { get; set; } = 0.22f;

    [JsonProperty("Lights")]
    public List<PropLightDef> Lights { get; set; } = null;

    public string BundlePath { get; set; }

    [JsonIgnore]
    public string ModelFilePath { get; set; }

    [JsonIgnore]
    public bool UsesFileForMesh {
      get {
        if (string.Equals(AssetSource, "File", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(AssetSource, "Auto", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(ModelFilePath)) return true;
        return false;
      }
    }

    [JsonIgnore]
    public bool UsesBundleForMesh {
      get {
        if (UsesFileForMesh) return false;
        if (string.Equals(AssetSource, "GameData", StringComparison.OrdinalIgnoreCase)) return false;
        if (string.Equals(AssetSource, "Bundle", StringComparison.OrdinalIgnoreCase)) return true;
        return IsMeshInBundle;
      }
    }

    public MeshTransformSettings BuildTransformSettings() {
      CorePivotMode pivot = CorePivotMode.Bottom;
      if (!string.IsNullOrEmpty(PivotMode)) {
        if (string.Equals(PivotMode, "Original", StringComparison.OrdinalIgnoreCase)) pivot = CorePivotMode.Original;
        else if (string.Equals(PivotMode, "Center", StringComparison.OrdinalIgnoreCase)) pivot = CorePivotMode.Center;
      }
      return new MeshTransformSettings {
        PivotMode = pivot,
        RotationOffset = new Float3(RotationOffset.x, RotationOffset.y, RotationOffset.z),
        PositionOffset = new Float3(PositionOffset.x, PositionOffset.y, PositionOffset.z),
        TargetHeight = TargetHeight,
        Scale = new Float3(1f, 1f, 1f) // PropModelDef.Scale is still applied on the GameObject transform
      };
    }

    public CoreColliderMode GetColliderMode() {
      if (string.Equals(ColliderMode, "Box", StringComparison.OrdinalIgnoreCase)) return CoreColliderMode.Box;
      if (string.Equals(ColliderMode, "Convex", StringComparison.OrdinalIgnoreCase)) return CoreColliderMode.Convex;
      return CoreColliderMode.Mesh;
    }

    public long GetMeshPathId(string slot) {
      if (MeshPathIds == null || string.IsNullOrEmpty(slot)) return 0;
      return MeshPathIds.TryGetValue(slot, out long pid) ? pid : 0;
    }

    public MeshPivotMode GetEffectivePivotMode(bool isFlimsyBase) {
      if (!string.IsNullOrEmpty(PivotMode)) {
        if (string.Equals(PivotMode, "Center", StringComparison.OrdinalIgnoreCase)) return MeshPivotMode.Center;
        if (string.Equals(PivotMode, "Bottom", StringComparison.OrdinalIgnoreCase)) return MeshPivotMode.Bottom;
        return MeshPivotMode.Original;
      }
      return isFlimsyBase && ChangePivotToCenterIfFlimsyMeshFormat ? MeshPivotMode.Center : MeshPivotMode.Original;
    }

    [JsonIgnore]
    public bool IsCompleteBundle {
      get {
        return !string.IsNullOrEmpty(CompleteBundleName);
      }
    }

    [JsonIgnore]
    public DestructibleObject.DestructibleSize DestructibleSize {
      get {
        return Enum.TryParse(destructibleSize, true, out DestructibleObject.DestructibleSize parsed)
          ? parsed
          : DestructibleObject.DestructibleSize.large;
      }
    }

    [JsonProperty("DestructibleMaterial")]
    public string destructibleMaterial { get; set; } = "metal";

    [JsonIgnore]
    public DestructibleObject.DestructibleMaterial DestructibleMaterial {
      get {
        return Enum.TryParse(destructibleMaterial, true, out DestructibleObject.DestructibleMaterial parsed)
          ? parsed
          : DestructibleObject.DestructibleMaterial.metal;
      }
    }

    [JsonProperty("FlimsyDestructibleType")]
    public string flimsyDestructibleType { get; set; } = "largeMetal";

    [JsonIgnore]
    public FlimsyDestructType FlimsyDestructibleType {
      get {
        return Enum.TryParse(flimsyDestructibleType, true, out FlimsyDestructType parsed)
          ? parsed
          : FlimsyDestructType.largeMetal;
      }
    }
  }
}