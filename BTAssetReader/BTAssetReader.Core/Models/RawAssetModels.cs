using System;
using System.Collections.Generic;

namespace BTAssetReader.Core.Models {
  public readonly struct AssetRef : IEquatable<AssetRef> {
    public readonly int FileIndex;
    public readonly long PathId;

    public bool IsNull => PathId == 0 || FileIndex < 0;

    public AssetRef(int fileIndex, long pathId) {
      FileIndex = fileIndex;
      PathId = pathId;
    }

    public bool Equals(AssetRef other) => FileIndex == other.FileIndex && PathId == other.PathId;
    public override bool Equals(object obj) => obj is AssetRef other && Equals(other);
    public override int GetHashCode() {
      unchecked {
        return (FileIndex * 397) ^ PathId.GetHashCode();
      }
    }
    public override string ToString() => $"({FileIndex}:{PathId})";
  }

  public struct PPtr {
    public int FileId;
    public long PathId;

    public bool IsNull => PathId == 0;

    public PPtr(int fileId, long pathId) {
      FileId = fileId;
      PathId = pathId;
    }
  }

  public struct Float2 {
    public float X, Y;
    public Float2(float x, float y) { X = x; Y = y; }
  }

  public struct Float3 {
    public float X, Y, Z;
    public Float3(float x, float y, float z) { X = x; Y = y; Z = z; }
  }

  public struct Float4 {
    public float X, Y, Z, W;
    public Float4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
  }

  public struct ColorRgba32 {
    public byte R, G, B, A;
    public ColorRgba32(byte r, byte g, byte b, byte a) { R = r; G = g; B = b; A = a; }
  }

  public struct RawBounds {
    public Float3 Center;
    public Float3 Extents;
  }

  public struct RawSubMesh {
    public uint FirstByte;
    public uint IndexCount;
    public int Topology;
    public uint BaseVertex;
    public uint FirstVertex;
    public uint VertexCount;
    public RawBounds LocalAabb;
  }

  public class RawMeshData {
    public string Name;
    public AssetRef Ref;
    public int VertexCount;
    public int IndexFormat; // 0 = UInt16, 1 = UInt32
    public RawSubMesh[] SubMeshes;
    public int[][] SubMeshIndices;
    public Float3[] Vertices;
    public Float3[] Normals;
    public Float4[] Tangents;
    public ColorRgba32[] Colors;
    public Float2[] Uv0;
    public Float2[] Uv1;
    public Float2[] Uv2;
    public RawBounds LocalAabb;
    public byte MeshCompression;
    public bool OriginalIsReadable;
  }

  public class RawTexture2DData {
    public string Name;
    public AssetRef Ref;
    public int Width;
    public int Height;
    public int CompleteImageSize;
    public int TextureFormat;
    public int MipCount;
    public bool IsReadable;
    public int FilterMode;
    public int Aniso;
    public float MipBias;
    public int WrapU;
    public int WrapV;
    public int WrapW;
    public int ColorSpace; // 0 = Linear, 1 = sRGB
    public byte[] RawBytes;
  }

  public class RawTexEnv {
    public string PropertyName;
    public AssetRef TextureRef;
    public Float2 Scale;
    public Float2 Offset;
  }

  public class RawMaterialData {
    public string Name;
    public AssetRef Ref;
    public AssetRef ShaderRef;
    public string ShaderName;
    public string[] ShaderKeywords;
    public uint LightmapFlags;
    public bool EnableInstancing;
    public bool DoubleSidedGI;
    public int CustomRenderQueue;
    public Dictionary<string, string> StringTagMap = new Dictionary<string, string>();
    public List<RawTexEnv> TexEnvs = new List<RawTexEnv>();
    public Dictionary<string, float> Floats = new Dictionary<string, float>();
    public Dictionary<string, Float4> Colors = new Dictionary<string, Float4>();
  }

  public class RawBoxColliderData {
    public bool Enabled;
    public bool IsTrigger;
    public Float3 Center;
    public Float3 Size;
  }

  public class RawMeshColliderData {
    public bool Enabled;
    public bool IsTrigger;
    public bool Convex;
    public AssetRef MeshRef;
  }

  public class RawRigidbodyData {
    public float Mass;
    public float Drag;
    public float AngularDrag;
    public bool UseGravity;
    public bool IsKinematic;
  }

  public class RawGameObjectNode {
    public string Name;
    public int Layer;
    public bool IsActive;
    public AssetRef GameObjectRef;
    public AssetRef TransformRef;
    public Float3 LocalPosition;
    public Float4 LocalRotation;
    public Float3 LocalScale;
    public AssetRef MeshRef;
    public string MeshName;
    public List<AssetRef> MaterialRefs = new List<AssetRef>();
    public List<string> MaterialNames = new List<string>();
    public RawBoxColliderData BoxCollider;
    public RawMeshColliderData MeshCollider;
    public RawRigidbodyData Rigidbody;
    public List<RawGameObjectNode> Children = new List<RawGameObjectNode>();
  }

  public static class UnityClassId {
    public const int GameObject = 1;
    public const int Transform = 4;
    public const int Material = 21;
    public const int MeshRenderer = 23;
    public const int Texture2D = 28;
    public const int MeshFilter = 33;
    public const int Mesh = 43;
    public const int Shader = 48;
    public const int Rigidbody = 54;
    public const int MeshCollider = 64;
    public const int BoxCollider = 65;
    public const int MonoBehaviour = 114;
    public const int MonoScript = 115;
    public const int LODGroup = 205;
  }
}
