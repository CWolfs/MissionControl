using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using BTAssetReader.Core.Decoders;
using BTAssetReader.Core.Geometry;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core {
  public sealed class RawMechReference {
    public string BundleName;
    public RawMeshData Mesh;
    public RawMaterialData PrimaryMaterial;
    public RawTexture2DData AlbedoTexture;
    public RawTexture2DData NormalTexture;
    public RawTexture2DData OcclusionTexture;
    public RawTexture2DData EmissionTexture;
    public float RawHeight;
    public float InGameHeight;
    public float ScaleMultiplier;
  }

  /// <summary>
  /// Loads an intact BattleTech mech prefab directly from BattleTech_Data/StreamingAssets/data/assetbundles/chrprfmech_*base-001
  /// in rest pose, combining active body-part SkinnedMeshRenderers and applying CombatGameConstants' TEST_MechScaleMultiplier.
  /// Pure zero-dependency C# compatible with both net471 and net8.0.
  /// </summary>
  public static class MechBundleLoader {
    private const int ClassIdSkinnedMeshRenderer = 137;
    private static readonly Regex MechScaleRegex = new Regex(
      "\"TEST_MechScaleMultiplier\"\\s*:\\s*([0-9]+(?:\\.[0-9]+)?)",
      RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Row-vector 4x4 matrix (translation in M41, M42, M43; matches Unity Transpose(M_col)).</summary>
    private struct RowMat4 {
      public float M11, M12, M13, M14;
      public float M21, M22, M23, M24;
      public float M31, M32, M33, M34;
      public float M41, M42, M43, M44;

      public static readonly RowMat4 Identity = new RowMat4 {
        M11 = 1f, M22 = 1f, M33 = 1f, M44 = 1f
      };

      public static RowMat4 CreateTrs(Float3 pos, Float4 q, Float3 scale) {
        float xx = q.X * q.X, yy = q.Y * q.Y, zz = q.Z * q.Z;
        float xy = q.X * q.Y, xz = q.X * q.Z, yz = q.Y * q.Z;
        float wx = q.W * q.X, wy = q.W * q.Y, wz = q.W * q.Z;

        // Row-vector rotation matrix = Transpose(Unity col-vector rotation matrix)
        float r11 = 1f - 2f * (yy + zz), r12 = 2f * (xy + wz), r13 = 2f * (xz - wy);
        float r21 = 2f * (xy - wz), r22 = 1f - 2f * (xx + zz), r23 = 2f * (yz + wx);
        float r31 = 2f * (xz + wy), r32 = 2f * (yz - wx), r33 = 1f - 2f * (xx + yy);

        // Scale * Rotation * Translation (row-vector order: v * S * R * T)
        return new RowMat4 {
          M11 = scale.X * r11, M12 = scale.X * r12, M13 = scale.X * r13, M14 = 0f,
          M21 = scale.Y * r21, M22 = scale.Y * r22, M23 = scale.Y * r23, M24 = 0f,
          M31 = scale.Z * r31, M32 = scale.Z * r32, M33 = scale.Z * r33, M34 = 0f,
          M41 = pos.X, M42 = pos.Y, M43 = pos.Z, M44 = 1f
        };
      }

      public static RowMat4 Multiply(RowMat4 a, RowMat4 b) => new RowMat4 {
        M11 = a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
        M12 = a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
        M13 = a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
        M14 = a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

        M21 = a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
        M22 = a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
        M23 = a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
        M24 = a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

        M31 = a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
        M32 = a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
        M33 = a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
        M34 = a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

        M41 = a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
        M42 = a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
        M43 = a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
        M44 = a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44
      };

      public Float3 TransformPoint(Float3 v) => new Float3(
        v.X * M11 + v.Y * M21 + v.Z * M31 + M41,
        v.X * M12 + v.Y * M22 + v.Z * M32 + M42,
        v.X * M13 + v.Y * M23 + v.Z * M33 + M43);

      public Float3 TransformNormal(Float3 n) {
        // 3x3 cofactor matrix (proportional to inverse-transpose; preserves normal direction even under non-uniform scale)
        float c11 = M22 * M33 - M23 * M32;
        float c12 = -(M21 * M33 - M23 * M31);
        float c13 = M21 * M32 - M22 * M31;
        float c21 = -(M12 * M33 - M13 * M32);
        float c22 = M11 * M33 - M13 * M31;
        float c23 = -(M11 * M32 - M12 * M31);
        float c31 = M12 * M23 - M13 * M22;
        float c32 = -(M11 * M23 - M13 * M21);
        float c33 = M11 * M22 - M12 * M21;

        float nx = n.X * c11 + n.Y * c21 + n.Z * c31;
        float ny = n.X * c12 + n.Y * c22 + n.Z * c32;
        float nz = n.X * c13 + n.Y * c23 + n.Z * c33;
        float len = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
        if (len > 1e-6f) {
          float inv = 1f / len;
          return new Float3(nx * inv, ny * inv, nz * inv);
        }
        return new Float3(0f, 1f, 0f);
      }

      public float Determinant3x3() =>
        M11 * (M22 * M33 - M23 * M32) -
        M12 * (M21 * M33 - M23 * M31) +
        M13 * (M21 * M32 - M22 * M31);
    }

    public static float ReadCombatMechScaleMultiplier(string battleTechDataPath) {
      if (string.IsNullOrWhiteSpace(battleTechDataPath)) return 1.25f;
      try {
        string constantsPath = Path.Combine(battleTechDataPath, "StreamingAssets", "data", "constants", "CombatGameConstants.json");
        if (File.Exists(constantsPath)) {
          string text = File.ReadAllText(constantsPath);
          Match m = MechScaleRegex.Match(text);
          if (m.Success && float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float mult) && mult > 0.1f && mult < 10f) {
            return mult;
          }
        }
      } catch { }
      return 1.25f;
    }

    public static string ResolveMechBundlePath(string battleTechDataPath, string bundleName) {
      if (string.IsNullOrWhiteSpace(battleTechDataPath) || string.IsNullOrWhiteSpace(bundleName)) return null;
      string dir = Path.Combine(battleTechDataPath, "StreamingAssets", "data", "assetbundles");
      string exact = Path.Combine(dir, bundleName);
      if (File.Exists(exact)) return exact;
      string lower = Path.Combine(dir, bundleName.ToLowerInvariant());
      if (File.Exists(lower)) return lower;
      return null;
    }

    public static RawMechReference Load(string battleTechDataPath, string bundleName, bool loadTextures = true) {
      string bundlePath = ResolveMechBundlePath(battleTechDataPath, bundleName);
      if (bundlePath == null) return null;

      float scaleMult = ReadCombatMechScaleMultiplier(battleTechDataPath);

      using (var bundle = UnityBundleReader.Open(bundlePath)) {
        UnityBundleReader.DirectoryEntry? primaryOpt = bundle.FindPrimarySerializedFile();
        if (!primaryOpt.HasValue) return null;

        using (Stream s = bundle.OpenEntryStream(primaryOpt.Value)) {
          SerializedFileHeader hdr = SerializedFileReader.ReadHeader(s, primaryOpt.Value.Path, 0);
          if (hdr == null) return null;

          var gos = new Dictionary<long, RawGameObjectInfo>();
          var tfs = new Dictionary<long, RawTransformInfo>();
          foreach (var o in hdr.ObjectList) {
            if (o.ClassId == UnityClassId.GameObject) {
              gos[o.PathId] = HierarchyDecoder.DecodeGameObject(SerializedFileReader.ReadObjectBytes(s, hdr, o), hdr.BigEndian);
            } else if (o.ClassId == UnityClassId.Transform) {
              tfs[o.PathId] = HierarchyDecoder.DecodeTransform(SerializedFileReader.ReadObjectBytes(s, hdr, o), hdr.BigEndian);
            }
          }

          long mainRootTf = 0;
          foreach (var kv in tfs) {
            if (kv.Value.Father.PathId != 0) continue;
            if (gos.TryGetValue(kv.Value.GameObject.PathId, out var rgo) &&
                rgo.Name != null &&
                rgo.Name.StartsWith("chrPrfMech", StringComparison.OrdinalIgnoreCase)) {
              mainRootTf = kv.Key;
              break;
            }
          }
          if (mainRootTf == 0) return null;

          var worldMats = new Dictionary<long, RowMat4>();
          void BuildWorld(long tfId, RowMat4 parent) {
            if (!tfs.TryGetValue(tfId, out var tf)) return;
            RowMat4 local = RowMat4.CreateTrs(tf.LocalPosition, tf.LocalRotation, tf.LocalScale);
            RowMat4 w = RowMat4.Multiply(local, parent);
            worldMats[tfId] = w;
            foreach (var ch in tf.Children) BuildWorld(ch.PathId, w);
          }
          BuildWorld(mainRootTf, RowMat4.Identity);

          var verts = new List<Float3>(16384);
          var norms = new List<Float3>(16384);
          var uvs = new List<Float2>(16384);
          var indices = new List<int>(32768);
          bool hasAnyUv = false;
          bool hasAnyNorm = false;

          long primaryMatPathId = 0;
          float rawMinY = float.MaxValue;
          float rawMaxY = float.MinValue;

          void WalkActiveParts(long tfId, bool parentActive) {
            if (!tfs.TryGetValue(tfId, out var tf)) return;
            if (!gos.TryGetValue(tf.GameObject.PathId, out var go)) return;
            bool isActive = parentActive && go.IsActive;
            if (!isActive) return;

            string lowName = (go.Name ?? "").ToLowerInvariant();
            if (lowName.Contains("dmg") || lowName.Contains("explode") || lowName.Contains("blip") ||
                lowName.Contains("decal") || lowName.Contains("vfx") || lowName.Contains("collider")) {
              return;
            }

            foreach (var comp in go.Components) {
              if (comp.FileId != 0 || !hdr.Objects.TryGetValue(comp.PathId, out var co)) continue;
              if (co.ClassId != ClassIdSkinnedMeshRenderer) continue;

              byte[] smrBytes = SerializedFileReader.ReadObjectBytes(s, hdr, co);
              if (!TryParseSkinnedMeshRenderer(smrBytes, hdr.BigEndian, out long meshPathId, out long firstMatPathId, out long firstBoneTfId)) {
                continue;
              }
              if (primaryMatPathId == 0 && firstMatPathId != 0) primaryMatPathId = firstMatPathId;
              if (meshPathId == 0 || !hdr.Objects.TryGetValue(meshPathId, out var meshObj)) continue;

              byte[] meshBytes = SerializedFileReader.ReadObjectBytes(s, hdr, meshObj);
              RowMat4 bindPoseRow = ReadFirstBindPoseRowMatrix(meshBytes, hdr.BigEndian);
              RawMeshData partMesh = MeshDecoder.Decode(
                meshBytes,
                hdr.BigEndian,
                new AssetRef(0, meshObj.PathId),
                battleTechDataPath,
                (sp, off, sz) => bundle.ReadStreamResource(sp, off, sz));

              if (partMesh == null || partMesh.Vertices == null || partMesh.Vertices.Length == 0 || partMesh.SubMeshIndices == null) continue;

              RowMat4 rawXform = (firstBoneTfId != 0 && worldMats.TryGetValue(firstBoneTfId, out var boneWorld))
                ? RowMat4.Multiply(bindPoseRow, boneWorld)
                : (worldMats.TryGetValue(tfId, out var selfWorld) ? selfWorld : RowMat4.Identity);

              bool flipWinding = rawXform.Determinant3x3() < 0f;

              int baseVert = verts.Count;
              bool partHasN = partMesh.Normals != null && partMesh.Normals.Length == partMesh.Vertices.Length;
              bool partHasU = partMesh.Uv0 != null && partMesh.Uv0.Length == partMesh.Vertices.Length;
              if (partHasN) hasAnyNorm = true;
              if (partHasU) hasAnyUv = true;

              for (int i = 0; i < partMesh.Vertices.Length; i++) {
                Float3 vRaw = rawXform.TransformPoint(partMesh.Vertices[i]);
                if (vRaw.Y < rawMinY) rawMinY = vRaw.Y;
                if (vRaw.Y > rawMaxY) rawMaxY = vRaw.Y;

                verts.Add(new Float3(vRaw.X * scaleMult, vRaw.Y * scaleMult, vRaw.Z * scaleMult));

                if (partHasN) {
                  norms.Add(rawXform.TransformNormal(partMesh.Normals[i]));
                } else {
                  norms.Add(new Float3(0f, 1f, 0f));
                }

                if (partHasU) uvs.Add(partMesh.Uv0[i]);
                else uvs.Add(new Float2(0f, 0f));
              }

              foreach (int[] sub in partMesh.SubMeshIndices) {
                if (sub == null) continue;
                for (int i = 0; i + 2 < sub.Length; i += 3) {
                  int a = baseVert + sub[i];
                  int b = baseVert + sub[i + 1];
                  int c = baseVert + sub[i + 2];
                  if (flipWinding) {
                    indices.Add(a); indices.Add(c); indices.Add(b);
                  } else {
                    indices.Add(a); indices.Add(b); indices.Add(c);
                  }
                }
              }
            }

            foreach (var ch in tf.Children) WalkActiveParts(ch.PathId, isActive);
          }

          WalkActiveParts(mainRootTf, true);
          if (verts.Count == 0 || indices.Count == 0) return null;

          // Ensure feet sit cleanly on Y = 0
          float shiftY = rawMinY < float.MaxValue ? -rawMinY * scaleMult : 0f;
          if (Math.Abs(shiftY) > 0.001f && Math.Abs(shiftY) < 1.5f) {
            for (int i = 0; i < verts.Count; i++) {
              Float3 p = verts[i];
              verts[i] = new Float3(p.X, p.Y + shiftY, p.Z);
            }
          }

          int[] triArray = indices.ToArray();
          var combined = new RawMeshData {
            Name = bundleName,
            VertexCount = verts.Count,
            IndexFormat = verts.Count > 65535 ? 1 : 0,
            Vertices = verts.ToArray(),
            Normals = hasAnyNorm ? norms.ToArray() : null,
            Uv0 = hasAnyUv ? uvs.ToArray() : null,
            SubMeshes = new[] { new RawSubMesh { Topology = 0, IndexCount = (uint)triArray.Length, VertexCount = (uint)verts.Count } },
            SubMeshIndices = new[] { triArray },
            OriginalIsReadable = true
          };
          MeshNormaliser.RecalculateNormalsIfMissing(combined);
          MeshNormaliser.RecalculateTangentsIfMissing(combined);
          MeshNormaliser.RecalculateBounds(combined);

          RawMaterialData primaryMat = null;
          RawTexture2DData albedoTex = null, normalTex = null, occTex = null, emTex = null;

          if (primaryMatPathId != 0 && hdr.Objects.TryGetValue(primaryMatPathId, out var matObj)) {
            primaryMat = MaterialDecoder.Decode(
              SerializedFileReader.ReadObjectBytes(s, hdr, matObj),
              hdr.BigEndian,
              new AssetRef(0, matObj.PathId),
              p => new AssetRef(p.FileId, p.PathId),
              null);

            if (loadTextures && primaryMat?.TexEnvs != null) {
              RawTexture2DData DecodeBundleTex(AssetRef tref) {
                if (tref.IsNull || tref.FileIndex != 0 || !hdr.Objects.TryGetValue(tref.PathId, out var tobj)) return null;
                try {
                  return Texture2DDecoder.Decode(
                    SerializedFileReader.ReadObjectBytes(s, hdr, tobj),
                    hdr.BigEndian,
                    tref,
                    battleTechDataPath,
                    (sp, off, sz) => bundle.ReadStreamResource(sp, off, sz));
                } catch {
                  return null;
                }
              }

              foreach (var te in primaryMat.TexEnvs) {
                if (te.TextureRef.IsNull) continue;
                switch (te.PropertyName) {
                  case "_MainTex": albedoTex ??= DecodeBundleTex(te.TextureRef); break;
                  case "_BumpMap": normalTex ??= DecodeBundleTex(te.TextureRef); break;
                  case "_OcclusionMap": occTex ??= DecodeBundleTex(te.TextureRef); break;
                  case "_EmissionMap": emTex ??= DecodeBundleTex(te.TextureRef); break;
                }
              }
            }
          }

          float rawH = rawMaxY > rawMinY ? (rawMaxY - rawMinY) : combined.LocalAabb.Extents.Y * 2f / scaleMult;
          return new RawMechReference {
            BundleName = bundleName,
            Mesh = combined,
            PrimaryMaterial = primaryMat,
            AlbedoTexture = albedoTex,
            NormalTexture = normalTex,
            OcclusionTexture = occTex,
            EmissionTexture = emTex,
            RawHeight = rawH,
            InGameHeight = rawH * scaleMult,
            ScaleMultiplier = scaleMult
          };
        }
      }
    }

    private static bool TryParseSkinnedMeshRenderer(byte[] data, bool bigEndian, out long meshPathId, out long firstMatPathId, out long firstBoneTfId) {
      meshPathId = 0;
      firstMatPathId = 0;
      firstBoneTfId = 0;
      if (data == null || data.Length < 120) return false;

      var r = new BinaryStreamReader(data, 0, bigEndian);
      r.Position = 64;
      int matCount = r.ReadInt32();
      if (matCount < 0 || matCount > 64) return false;
      for (int i = 0; i < matCount; i++) {
        PPtr mp = r.ReadPPtr();
        if (i == 0 && mp.FileId == 0) firstMatPathId = mp.PathId;
      }
      r.Skip(4 + 12 + 12 + 12 + 8 + 4);
      r.ReadBool();
      r.ReadBool();
      r.Align4();
      PPtr meshPtr = r.ReadPPtr();
      if (meshPtr.FileId != 0) return false;
      meshPathId = meshPtr.PathId;

      int boneCount = r.ReadInt32();
      if (boneCount > 0 && boneCount < 512) {
        PPtr b0 = r.ReadPPtr();
        if (b0.FileId == 0) firstBoneTfId = b0.PathId;
      }
      return meshPathId != 0;
    }

    private static RowMat4 ReadFirstBindPoseRowMatrix(byte[] meshBytes, bool bigEndian) {
      try {
        var r = new BinaryStreamReader(meshBytes, 0, bigEndian);
        r.ReadAlignedString();
        int subMeshCount = r.ReadInt32();
        r.Skip(subMeshCount * 48);
        int shapeVertCount = r.ReadInt32();
        r.Skip(shapeVertCount * 40);
        int shapeCount = r.ReadInt32();
        r.Skip(shapeCount * 12);
        int shapeChannelCount = r.ReadInt32();
        for (int i = 0; i < shapeChannelCount; i++) {
          r.ReadAlignedString();
          r.Skip(12);
        }
        int fullWeightCount = r.ReadInt32();
        r.Skip(fullWeightCount * 4);
        int bindPoseCount = r.ReadInt32();
        if (bindPoseCount <= 0) return RowMat4.Identity;

        // Unity serializes Matrix4x4 in column-major order (e00,e10,e20,e30, ...);
        // Transpose(colMajor) maps straight into our row-vector RowMat4!
        float[] f = new float[16];
        for (int i = 0; i < 16; i++) f[i] = r.ReadSingle();
        return new RowMat4 {
          M11 = f[0], M12 = f[4], M13 = f[8],  M14 = f[12],
          M21 = f[1], M22 = f[5], M23 = f[9],  M24 = f[13],
          M31 = f[2], M32 = f[6], M33 = f[10], M34 = f[14],
          M41 = f[3], M42 = f[7], M43 = f[11], M44 = f[15]
        };
      } catch {
        return RowMat4.Identity;
      }
    }
  }
}
