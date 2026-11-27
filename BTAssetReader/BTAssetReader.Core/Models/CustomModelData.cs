using System;
using System.Collections.Generic;

namespace BTAssetReader.Core.Models {
  public enum CorePivotMode {
    Original = 0,
    Center = 1,
    Bottom = 2
  }

  public enum CoreColliderMode {
    Mesh = 0,
    Box = 1,
    Convex = 2
  }

  public class CustomPbrMaterialData {
    public string Name = "Material";
    public Float4 BaseColorFactor = new Float4(1f, 1f, 1f, 1f);
    public float MetallicFactor = 0.0f;
    public float RoughnessFactor = 0.8f;
    public float NormalScale = 1.0f;
    public Float3 EmissiveFactor = new Float3(0f, 0f, 0f);
    public string AlphaMode = "OPAQUE"; // OPAQUE, MASK, BLEND
    public float AlphaCutoff = 0.5f;
    public bool DoubleSided = false;

    // Raw encoded image bytes (PNG/JPG) so Unity and Godot decode natively
    public byte[] BaseColorImageBytes;
    public string BaseColorImageMime;
    public byte[] MetallicRoughnessImageBytes;
    public string MetallicRoughnessImageMime;
    public byte[] NormalImageBytes;
    public string NormalImageMime;
    public byte[] OcclusionImageBytes;
    public string OcclusionImageMime;
    public byte[] EmissiveImageBytes;
    public string EmissiveImageMime;
  }

  public class PropLightData {
    public string Name = "Light";
    public string Type = "Point"; // Point, Spot, Linear, Area
    public Float3 Position = new Float3(0f, 2f, 0f);
    public Float3 Rotation = new Float3(0f, 0f, 0f); // Unity Euler degrees
    public Float4 Color = new Float4(1f, 0.95f, 0.85f, 1f);
    public float Intensity = 1.0f; // 1.0 maps to BT illuminance scale (3000)
    public float Radius = 15.0f;
    public float InnerAngle = 30.0f;
    public float OuterAngle = 50.0f;
    public float Length = 2.0f;
    public bool CastShadows = false;
    public bool ContributeSpecular = true;
    public bool ContributeVolumetrics = false;

    public PropLightData Clone() {
      return new PropLightData {
        Name = Name,
        Type = Type,
        Position = Position,
        Rotation = Rotation,
        Color = Color,
        Intensity = Intensity,
        Radius = Radius,
        InnerAngle = InnerAngle,
        OuterAngle = OuterAngle,
        Length = Length,
        CastShadows = CastShadows,
        ContributeSpecular = ContributeSpecular,
        ContributeVolumetrics = ContributeVolumetrics
      };
    }
  }

  public class FracturePieceData {
    public string Name;
    public Float3 Centroid;
    public RawMeshData Mesh;
    public RawBounds LocalBounds;
    public float EstimatedMass = 5000f;
    public int InteriorSubMeshIndex = -1;
  }

  public class ModelPartInfo {
    public string Name;
    public int TriangleCount;
    public RawBounds LocalBounds;

    public ModelPartInfo Clone() => new ModelPartInfo {
      Name = Name,
      TriangleCount = TriangleCount,
      LocalBounds = LocalBounds
    };
  }

  public class MeshTransformSettings {
    public CorePivotMode PivotMode = CorePivotMode.Bottom;
    public Float3 RotationOffset = new Float3(0f, 0f, 0f);
    public Float3 PositionOffset = new Float3(0f, 0f, 0f);
    public float TargetHeight = 0f;
    public Float3 Scale = new Float3(1f, 1f, 1f);
    public bool FlipTextureV = false;
    public List<string> ExcludedParts;
  }

  public class CustomModelBundle {
    public string Name;
    public string SourceFilePath;
    public bool UsedLooseTextureFallback;
    public RawMeshData Lod0Mesh;
    public RawMeshData Lod1Mesh;
    public RawMeshData Lod2Mesh;
    public RawMeshData ColMesh;
    public RawMeshData GlassMesh;
    public RawMeshData ShellMesh;
    public RawMeshData ShellColMesh;
    public List<FracturePieceData> CustomSplits = new List<FracturePieceData>();
    public List<CustomPbrMaterialData> Materials = new List<CustomPbrMaterialData>();
    public List<PropLightData> Lights = new List<PropLightData>();

    /// <summary>
    /// Creates a copy whose mesh geometry and light positions can be normalised in-place without
    /// mutating the raw source bundle, while sharing immutable material and texture byte buffers.
    /// </summary>
    public CustomModelBundle CloneForNormalization() {
      var copy = new CustomModelBundle {
        Name = Name,
        SourceFilePath = SourceFilePath,
        UsedLooseTextureFallback = UsedLooseTextureFallback,
        Lod0Mesh = CloneMesh(Lod0Mesh),
        Lod1Mesh = CloneMesh(Lod1Mesh),
        Lod2Mesh = CloneMesh(Lod2Mesh),
        ColMesh = CloneMesh(ColMesh),
        GlassMesh = CloneMesh(GlassMesh),
        ShellMesh = CloneMesh(ShellMesh),
        ShellColMesh = CloneMesh(ShellColMesh),
        Materials = Materials != null ? new List<CustomPbrMaterialData>(Materials) : new List<CustomPbrMaterialData>()
      };
      if (CustomSplits != null) {
        foreach (var fp in CustomSplits) {
          if (fp == null) continue;
          copy.CustomSplits.Add(new FracturePieceData {
            Name = fp.Name,
            Centroid = fp.Centroid,
            Mesh = CloneMesh(fp.Mesh),
            LocalBounds = fp.LocalBounds,
            EstimatedMass = fp.EstimatedMass,
            InteriorSubMeshIndex = fp.InteriorSubMeshIndex
          });
        }
      }
      if (Lights != null) {
        foreach (var l in Lights) {
          if (l != null) copy.Lights.Add(l.Clone());
        }
      }
      return copy;
    }

    private static RawMeshData CloneMesh(RawMeshData src) {
      if (src == null) return null;
      int[][] subIndices = null;
      if (src.SubMeshIndices != null) {
        subIndices = new int[src.SubMeshIndices.Length][];
        for (int i = 0; i < src.SubMeshIndices.Length; i++) {
          if (src.SubMeshIndices[i] != null) subIndices[i] = (int[])src.SubMeshIndices[i].Clone();
        }
      }
      return new RawMeshData {
        Name = src.Name,
        Ref = src.Ref,
        VertexCount = src.VertexCount,
        IndexFormat = src.IndexFormat,
        SubMeshes = src.SubMeshes != null ? (RawSubMesh[])src.SubMeshes.Clone() : null,
        SubMeshIndices = subIndices,
        Vertices = src.Vertices != null ? (Float3[])src.Vertices.Clone() : null,
        Normals = src.Normals != null ? (Float3[])src.Normals.Clone() : null,
        Tangents = src.Tangents != null ? (Float4[])src.Tangents.Clone() : null,
        Colors = src.Colors,
        Uv0 = src.Uv0 != null ? (Float2[])src.Uv0.Clone() : null,
        Uv1 = src.Uv1,
        Uv2 = src.Uv2,
        LocalAabb = src.LocalAabb,
        MeshCompression = src.MeshCompression,
        OriginalIsReadable = src.OriginalIsReadable,
        Parts = src.Parts != null ? src.Parts.ConvertAll(p => p.Clone()) : null,
        VertexPartIds = src.VertexPartIds != null ? (int[])src.VertexPartIds.Clone() : null
      };
    }
  }
}
