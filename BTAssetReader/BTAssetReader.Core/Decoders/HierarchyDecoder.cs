using System;
using System.Collections.Generic;
using System.IO;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.Decoders {
  public struct RawGameObjectInfo {
    public string Name;
    public int Layer;
    public bool IsActive;
    public List<PPtr> Components;
  }

  public struct RawTransformInfo {
    public PPtr GameObject;
    public Float4 LocalRotation;
    public Float3 LocalPosition;
    public Float3 LocalScale;
    public List<PPtr> Children;
    public PPtr Father;
  }

  public static class HierarchyDecoder {
    public static RawGameObjectInfo DecodeGameObject(byte[] data, bool bigEndian) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      int compCount = r.ReadInt32();
      var comps = new List<PPtr>(compCount);
      for (int i = 0; i < compCount; i++) {
        comps.Add(r.ReadPPtr());
      }
      int layer = r.ReadInt32();
      string name = r.ReadAlignedString();
      r.ReadUInt16(); // m_Tag
      bool isActive = r.ReadBool();
      return new RawGameObjectInfo {
        Name = name,
        Layer = layer,
        IsActive = isActive,
        Components = comps
      };
    }

    public static string PeekGameObjectName(FileStream fs, SerializedFileHeader header, SerializedObjectInfo obj) {
      long absOffset = (long)header.DataOffset + obj.ByteStart;
      int peekLen = (int)Math.Min(obj.ByteSize, 512u);
      fs.Seek(absOffset, SeekOrigin.Begin);
      byte[] buf = new byte[peekLen];
      if (SerializedFileReader.ReadExact(fs, buf, 0, peekLen) < 12) return null;

      var r = new BinaryStreamReader(buf, 0, header.BigEndian);
      int compCount = r.ReadInt32();
      int nameOff = 4 + compCount * 12 + 4; // components + layer
      if (nameOff + 4 > peekLen) {
        // Rare GameObject with >40 components; read full object
        buf = SerializedFileReader.ReadObjectBytes(fs, header, obj);
        r = new BinaryStreamReader(buf, 0, header.BigEndian);
        compCount = r.ReadInt32();
        nameOff = 4 + compCount * 12 + 4;
      }
      if (nameOff + 4 > buf.Length) return null;
      r.Position = nameOff;
      int slen = r.ReadInt32();
      if (slen <= 0 || slen > 256 || r.Position + slen > buf.Length) return null;
      r.Position = nameOff;
      return r.ReadAlignedString();
    }

    public static RawTransformInfo DecodeTransform(byte[] data, bool bigEndian) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      PPtr go = r.ReadPPtr();
      Float4 rot = r.ReadFloat4();
      Float3 pos = r.ReadFloat3();
      Float3 scale = r.ReadFloat3();
      int childCount = r.ReadInt32();
      var children = new List<PPtr>(childCount);
      for (int i = 0; i < childCount; i++) {
        children.Add(r.ReadPPtr());
      }
      PPtr father = r.ReadPPtr();
      return new RawTransformInfo {
        GameObject = go,
        LocalRotation = rot,
        LocalPosition = pos,
        LocalScale = scale,
        Children = children,
        Father = father
      };
    }

    public static PPtr DecodeMeshFilterMesh(byte[] data, bool bigEndian) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      r.ReadPPtr(); // m_GameObject
      return r.ReadPPtr(); // m_Mesh
    }

    public static List<PPtr> DecodeMeshRendererMaterials(byte[] data, bool bigEndian) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      // Unity 2018.4 MeshRenderer layout before m_Materials:
      // m_GameObject(12) + enabled(1)+cast(1)+recv(1)+dynOcc(1)+motion(1)+lp(1)+rp(1)+pad(1) [8]
      // + renderingLayerMask(4) + rendererPriority(4) + lmIdx(2) + lmIdxDyn(2)
      // + lmTilingOffset(16) + lmTilingOffsetDyn(16) = 64 bytes
      if (data.Length < 68) return new List<PPtr>();
      r.Position = 64;
      int matCount = r.ReadInt32();
      if (matCount < 0 || matCount > 128 || 68 + matCount * 12 > data.Length) {
        return new List<PPtr>();
      }
      var mats = new List<PPtr>(matCount);
      for (int i = 0; i < matCount; i++) {
        mats.Add(r.ReadPPtr());
      }
      return mats;
    }

    public static RawBoxColliderData DecodeBoxCollider(byte[] data, bool bigEndian) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      r.ReadPPtr(); // m_GameObject
      r.ReadPPtr(); // m_Material
      bool isTrigger = r.ReadBool();
      bool enabled = r.ReadBool();
      r.Align4();
      Float3 size = r.ReadFloat3();
      Float3 center = r.ReadFloat3();
      return new RawBoxColliderData {
        IsTrigger = isTrigger,
        Enabled = enabled,
        Size = size,
        Center = center
      };
    }

    public static RawMeshColliderData DecodeMeshCollider(byte[] data, bool bigEndian, Func<PPtr, AssetRef> resolvePPtr) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      r.ReadPPtr(); // m_GameObject
      r.ReadPPtr(); // m_Material
      bool isTrigger = r.ReadBool();
      bool enabled = r.ReadBool();
      r.Align4();
      bool convex = r.ReadBool();
      r.Align4();
      r.ReadInt32(); // m_CookingOptions
      r.ReadSingle(); // m_SkinWidth
      PPtr meshPtr = r.ReadPPtr();
      return new RawMeshColliderData {
        IsTrigger = isTrigger,
        Enabled = enabled,
        Convex = convex,
        MeshRef = resolvePPtr(meshPtr)
      };
    }

    public static RawRigidbodyData DecodeRigidbody(byte[] data, bool bigEndian) {
      var r = new BinaryStreamReader(data, 0, bigEndian);
      r.ReadPPtr(); // m_GameObject
      float mass = r.ReadSingle();
      float drag = r.ReadSingle();
      float angularDrag = r.ReadSingle();
      bool useGravity = r.ReadBool();
      bool isKinematic = r.ReadBool();
      return new RawRigidbodyData {
        Mass = mass,
        Drag = drag,
        AngularDrag = angularDrag,
        UseGravity = useGravity,
        IsKinematic = isKinematic
      };
    }
  }
}
