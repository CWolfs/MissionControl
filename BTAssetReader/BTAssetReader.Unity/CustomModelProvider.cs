using System;
using System.Collections.Generic;
using UnityEngine;
using BTAssetReader.Core.CustomModels;
using BTAssetReader.Core.Destruction;
using BTAssetReader.Core.Geometry;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Unity {
  public sealed class CustomUnityModelAsset {
    public string CacheKey;
    public string SourceFilePath;
    public RawMeshData RawLod0Mesh;
    public Mesh Lod0Mesh;
    public Mesh Lod1Mesh;
    public Mesh Lod2Mesh;
    public Mesh ColMesh;
    public Mesh GlassMesh;
    public Mesh ShellMesh;
    public Mesh ShellColMesh;
    public Material[] EmbeddedMaterials;
    public List<SplitPieceAsset> CustomSplits;
    public List<PropLightData> EmbeddedLights;
    public readonly List<UnityEngine.Object> OwnedObjects = new List<UnityEngine.Object>();
  }

  /// <summary>
  /// Runtime loader and cache for AssetSource="File" (.glb / .obj) custom props,
  /// embedded PBR-to-BattleTech-Standard material conversion, and cached procedural
  /// plane-sliced splits and Auto-Stump shells.
  /// </summary>
  public sealed class CustomModelProvider {
    private static CustomModelProvider _instance;
    public static CustomModelProvider Instance => _instance ?? (_instance = new CustomModelProvider());

    private readonly Dictionary<string, CustomUnityModelAsset> _models = new Dictionary<string, CustomUnityModelAsset>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<SplitPieceAsset>> _proceduralSplits = new Dictionary<string, List<SplitPieceAsset>>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Mesh> _autoStumpShells = new Dictionary<string, Mesh>(StringComparer.OrdinalIgnoreCase);

    public CustomUnityModelAsset GetOrLoad(
      string cacheKey,
      string filePath,
      MeshTransformSettings settings,
      CoreColliderMode colliderMode = CoreColliderMode.Mesh,
      bool makeReadable = true,
      Action<string> logWarning = null) {
      if (string.IsNullOrEmpty(filePath)) return null;
      string key = string.IsNullOrEmpty(cacheKey) ? filePath : cacheKey;

      if (_models.TryGetValue(key, out CustomUnityModelAsset existing) && existing != null && existing.Lod0Mesh != null) {
        return existing;
      }

      try {
        CustomModelBundle bundle = CustomModelLoader.Load(filePath);
        if (bundle == null || bundle.Lod0Mesh == null) return null;

        MeshNormaliser.Normalize(bundle, settings ?? new MeshTransformSettings());

        var asset = new CustomUnityModelAsset {
          CacheKey = key,
          SourceFilePath = filePath,
          RawLod0Mesh = bundle.Lod0Mesh,
          EmbeddedLights = bundle.Lights ?? new List<PropLightData>()
        };

        asset.Lod0Mesh = Track(asset, UnityMeshBuilder.Build(bundle.Lod0Mesh, makeReadable, MeshPivotMode.Original));
        asset.Lod1Mesh = Track(asset, UnityMeshBuilder.Build(bundle.Lod1Mesh, makeReadable, MeshPivotMode.Original));
        asset.Lod2Mesh = Track(asset, UnityMeshBuilder.Build(bundle.Lod2Mesh, makeReadable, MeshPivotMode.Original));
        asset.GlassMesh = Track(asset, UnityMeshBuilder.Build(bundle.GlassMesh, makeReadable, MeshPivotMode.Original));
        asset.ShellMesh = Track(asset, UnityMeshBuilder.Build(bundle.ShellMesh, makeReadable, MeshPivotMode.Original));
        asset.ShellColMesh = Track(asset, UnityMeshBuilder.Build(bundle.ShellColMesh, makeReadable, MeshPivotMode.Original));

        if (bundle.ColMesh != null) {
          asset.ColMesh = Track(asset, UnityMeshBuilder.Build(bundle.ColMesh, makeReadable, MeshPivotMode.Original));
        } else if (colliderMode == CoreColliderMode.Box) {
          RawMeshData boxRaw = MeshNormaliser.CreateBoxMesh(bundle.Lod0Mesh.LocalAabb, $"{bundle.Name}_COL");
          asset.ColMesh = Track(asset, UnityMeshBuilder.Build(boxRaw, makeReadable, MeshPivotMode.Original));
        } else {
          asset.ColMesh = asset.Lod0Mesh;
        }

        // Convert embedded PBR materials to BattleTech Standard
        int matCount = bundle.Materials?.Count ?? 0;
        asset.EmbeddedMaterials = new Material[matCount];
        for (int i = 0; i < matCount; i++) {
          asset.EmbeddedMaterials[i] = BuildBattleTechPbrMaterial(asset, bundle.Materials[i], logWarning);
        }

        // Convert artist-authored split_* nodes if present in the .glb/.obj
        if (bundle.CustomSplits != null && bundle.CustomSplits.Count > 0) {
          asset.CustomSplits = new List<SplitPieceAsset>(bundle.CustomSplits.Count);
          foreach (var piece in bundle.CustomSplits) {
            Mesh pm = Track(asset, UnityMeshBuilder.Build(piece.Mesh, makeReadable: false, pivotMode: MeshPivotMode.Original));
            if (pm == null) continue;
            asset.CustomSplits.Add(new SplitPieceAsset {
              Name = piece.Name,
              LocalPosition = new Vector3(piece.Centroid.X, piece.Centroid.Y, piece.Centroid.Z),
              LocalRotation = Quaternion.identity,
              LocalScale = Vector3.one,
              Mesh = pm,
              Materials = null, // Let BuildingFactory apply the prop's configured outerMaterials (including overrides/vanilla replacements)
              BoxCollider = new RawBoxColliderData {
                Enabled = true,
                Center = piece.LocalBounds.Center,
                Size = new Float3(piece.LocalBounds.Extents.X * 2f, piece.LocalBounds.Extents.Y * 2f, piece.LocalBounds.Extents.Z * 2f)
              },
              Mass = piece.EstimatedMass
            });
          }
        }

        _models[key] = asset;
        return asset;
      } catch (Exception ex) {
        logWarning?.Invoke($"[BTAssetReader.CustomModelProvider] Failed to load '{filePath}': {ex.Message}");
        return null;
      }
    }

    public List<SplitPieceAsset> GetOrBuildProceduralSplits(
      string cacheKey,
      RawMeshData rawSource,
      int fractureCount,
      int fractureSeed,
      Material[] outerMaterials,
      Material interiorMaterial,
      float excludeBelowStumpFraction = 0f) {
      if (rawSource == null) return null;
      string key = $"{cacheKey}|{fractureCount}|{fractureSeed}|{excludeBelowStumpFraction:F2}";

      if (_proceduralSplits.TryGetValue(key, out List<SplitPieceAsset> cached) && cached != null && cached.Count > 0 && cached[0].Mesh != null) {
        return cached;
      }

      List<FracturePieceData> rawPieces = PlaneSliceFracturer.Fracture(rawSource, fractureCount, fractureSeed, excludeBelowStumpFraction);
      if (rawPieces == null || rawPieces.Count == 0) return null;

      var result = new List<SplitPieceAsset>(rawPieces.Count);
      for (int i = 0; i < rawPieces.Count; i++) {
        FracturePieceData fp = rawPieces[i];
        Mesh pieceMesh = UnityMeshBuilder.Build(fp.Mesh, makeReadable: false, pivotMode: MeshPivotMode.Original);
        if (pieceMesh == null) continue;

        int subCount = pieceMesh.subMeshCount;
        var pieceMats = new Material[subCount];
        for (int s = 0; s < subCount; s++) {
          if (s == fp.InteriorSubMeshIndex && interiorMaterial != null) {
            pieceMats[s] = interiorMaterial;
          } else if (outerMaterials != null && outerMaterials.Length > 0) {
            pieceMats[s] = outerMaterials[Math.Min(s, outerMaterials.Length - 1)];
          } else {
            pieceMats[s] = interiorMaterial;
          }
        }

        result.Add(new SplitPieceAsset {
          Name = fp.Name,
          LocalPosition = new Vector3(fp.Centroid.X, fp.Centroid.Y, fp.Centroid.Z),
          LocalRotation = Quaternion.identity,
          LocalScale = Vector3.one,
          Mesh = pieceMesh,
          Materials = pieceMats,
          BoxCollider = new RawBoxColliderData {
            Enabled = true,
            Center = fp.LocalBounds.Center,
            Size = new Float3(fp.LocalBounds.Extents.X * 2f, fp.LocalBounds.Extents.Y * 2f, fp.LocalBounds.Extents.Z * 2f)
          },
          Mass = fp.EstimatedMass
        });
      }

      _proceduralSplits[key] = result;
      return result;
    }

    public Mesh GetOrBuildAutoStumpShell(string cacheKey, RawMeshData rawSource, float stumpHeight, int seed) {
      if (rawSource == null) return null;
      string key = $"{cacheKey}|{stumpHeight:F2}|{seed}";
      if (_autoStumpShells.TryGetValue(key, out Mesh cached) && cached != null) {
        return cached;
      }

      RawMeshData rawShell = AutoStumpShellGenerator.Generate(rawSource, stumpHeight, seed);
      if (rawShell == null) return null;

      Mesh shellMesh = UnityMeshBuilder.Build(rawShell, makeReadable: false, pivotMode: MeshPivotMode.Original);
      if (shellMesh != null) {
        _autoStumpShells[key] = shellMesh;
      }
      return shellMesh;
    }

    public static RawMeshData ExtractRawMeshFromUnityMesh(Mesh unityMesh) {
      if (unityMesh == null || !unityMesh.isReadable || unityMesh.vertexCount == 0) return null;

      Vector3[] uVerts = unityMesh.vertices;
      Vector3[] uNorms = unityMesh.normals;
      Vector2[] uUvs = unityMesh.uv;

      var verts = new Float3[uVerts.Length];
      for (int i = 0; i < uVerts.Length; i++) verts[i] = new Float3(uVerts[i].x, uVerts[i].y, uVerts[i].z);

      Float3[] norms = null;
      if (uNorms != null && uNorms.Length == uVerts.Length) {
        norms = new Float3[uNorms.Length];
        for (int i = 0; i < uNorms.Length; i++) norms[i] = new Float3(uNorms[i].x, uNorms[i].y, uNorms[i].z);
      }

      Float2[] uvs = null;
      if (uUvs != null && uUvs.Length == uVerts.Length) {
        uvs = new Float2[uUvs.Length];
        for (int i = 0; i < uUvs.Length; i++) uvs[i] = new Float2(uUvs[i].x, uUvs[i].y);
      }

      int subCount = unityMesh.subMeshCount;
      var subMeshes = new RawSubMesh[subCount];
      var subIndices = new int[subCount][];
      for (int s = 0; s < subCount; s++) {
        int[] tris = unityMesh.GetTriangles(s);
        subIndices[s] = tris;
        subMeshes[s] = new RawSubMesh { Topology = 0, IndexCount = (uint)tris.Length };
      }

      var raw = new RawMeshData {
        Name = unityMesh.name,
        VertexCount = verts.Length,
        IndexFormat = verts.Length > 65535 ? 1 : 0,
        Vertices = verts,
        Normals = norms,
        Uv0 = uvs,
        SubMeshes = subMeshes,
        SubMeshIndices = subIndices,
        OriginalIsReadable = true
      };
      MeshNormaliser.RecalculateBounds(raw);
      return raw;
    }

    private static Material BuildBattleTechPbrMaterial(CustomUnityModelAsset asset, CustomPbrMaterialData pbr, Action<string> logWarning) {
      Shader shader = Shader.Find("BattleTech Standard") ?? Shader.Find("Standard");
      var mat = new Material(shader) {
        name = pbr?.Name ?? "CustomPropMaterial"
      };
      Track(asset, mat);

      if (pbr == null) return mat;

      mat.SetColor("_Color", new Color(pbr.BaseColorFactor.X, pbr.BaseColorFactor.Y, pbr.BaseColorFactor.Z, pbr.BaseColorFactor.W));
      mat.SetFloat("_Metallic", Mathf.Clamp01(pbr.MetallicFactor));
      mat.SetFloat("_Glossiness", Mathf.Clamp01(1f - pbr.RoughnessFactor));

      Texture2D baseTex = DecodeImageTexture(asset, pbr.BaseColorImageBytes, $"{mat.name}_BaseColor", linear: false, logWarning);
      if (baseTex != null) {
        mat.SetTexture("_MainTex", baseTex);
      }

      if (pbr.NormalScale > 0.001f) {
        Texture2D normRaw = DecodeImageTexture(asset, pbr.NormalImageBytes, $"{mat.name}_NormalRaw", linear: true, logWarning);
        if (normRaw != null) {
          Texture2D packedNorm = ConvertRgbNormalToUnityDxt5nm(asset, normRaw, $"{mat.name}_Normal");
          mat.SetTexture("_BumpMap", packedNorm ?? normRaw);
          mat.SetFloat("_BumpScale", pbr.NormalScale);
          mat.EnableKeyword("_NORMALMAP");
        }
      }

      // glTF packs roughness in G and metallic in B; Unity/BT Standard expects metallic in R and smoothness (1-roughness) in A
      Texture2D mrRaw = DecodeImageTexture(asset, pbr.MetallicRoughnessImageBytes, $"{mat.name}_MRRaw", linear: true, logWarning);
      if (mrRaw != null) {
        Texture2D packed = ConvertGltfMetallicRoughnessToUnity(asset, mrRaw, pbr.MetallicFactor, pbr.RoughnessFactor, $"{mat.name}_MetallicGloss");
        if (packed != null) {
          mat.SetTexture("_MetallicGlossMap", packed);
          mat.EnableKeyword("_METALLICGLOSSMAP");
        }
      }

      Texture2D aoRaw = DecodeImageTexture(asset, pbr.OcclusionImageBytes, $"{mat.name}_AORaw", linear: true, logWarning);
      if (aoRaw != null) {
        // glTF stores occlusion in R; BattleTech Standard reads occlusion from G
        Texture2D packedAo = ConvertGltfOcclusionToUnity(asset, aoRaw, $"{mat.name}_AO");
        mat.SetTexture("_OcclusionMap", packedAo ?? aoRaw);
      }

      Texture2D emTex = DecodeImageTexture(asset, pbr.EmissiveImageBytes, $"{mat.name}_Emissive", linear: false, logWarning);
      bool hasEmissive = emTex != null || pbr.EmissiveFactor.X > 1e-4f || pbr.EmissiveFactor.Y > 1e-4f || pbr.EmissiveFactor.Z > 1e-4f;
      if (hasEmissive) {
        if (emTex != null) mat.SetTexture("_EmissionMap", emTex);
        Color ec = (pbr.EmissiveFactor.X + pbr.EmissiveFactor.Y + pbr.EmissiveFactor.Z > 1e-4f)
          ? new Color(pbr.EmissiveFactor.X, pbr.EmissiveFactor.Y, pbr.EmissiveFactor.Z, 1f)
          : Color.white;
        mat.SetColor("_EmissionColor", ec);
        mat.EnableKeyword("_EMISSION");
      }

      if (string.Equals(pbr.AlphaMode, "MASK", StringComparison.OrdinalIgnoreCase)) {
        mat.SetFloat("_Mode", 1f);
        mat.SetFloat("_Cutoff", pbr.AlphaCutoff);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.renderQueue = 2450;
      } else if (string.Equals(pbr.AlphaMode, "BLEND", StringComparison.OrdinalIgnoreCase)) {
        mat.SetFloat("_Mode", 3f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
      }

      return mat;
    }

    private static Texture2D DecodeImageTexture(CustomUnityModelAsset asset, byte[] bytes, string name, bool linear, Action<string> logWarning) {
      if (bytes == null || bytes.Length == 0) return null;
      try {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: true, linear: linear) {
          name = name,
          wrapMode = TextureWrapMode.Repeat,
          filterMode = FilterMode.Trilinear
        };
        if (ImageConversion.LoadImage(tex, bytes, markNonReadable: false)) {
          return Track(asset, tex);
        }
        UnityEngine.Object.Destroy(tex);
      } catch (Exception ex) {
        logWarning?.Invoke($"[BTAssetReader.CustomModelProvider] Failed to decode image '{name}': {ex.Message}");
      }
      return null;
    }

    private static Texture2D ConvertRgbNormalToUnityDxt5nm(CustomUnityModelAsset asset, Texture2D rgbNorm, string name) {
      try {
        Color32[] src = rgbNorm.GetPixels32();
        var dst = new Color32[src.Length];
        // Unity DX11 UnpackNormal expects DXT5nm layout: G = normal.y, A = normal.x
        for (int i = 0; i < src.Length; i++) {
          dst[i] = new Color32(255, src[i].g, 255, src[i].r);
        }
        var packed = new Texture2D(rgbNorm.width, rgbNorm.height, TextureFormat.RGBA32, mipChain: true, linear: true) {
          name = name,
          wrapMode = TextureWrapMode.Repeat,
          filterMode = FilterMode.Trilinear
        };
        packed.SetPixels32(dst);
        packed.Apply(updateMipmaps: true, makeNoLongerReadable: true);
        return Track(asset, packed);
      } catch {
        return null;
      }
    }

    private static Texture2D ConvertGltfOcclusionToUnity(CustomUnityModelAsset asset, Texture2D gltfAo, string name) {
      try {
        Color32[] src = gltfAo.GetPixels32();
        var dst = new Color32[src.Length];
        // glTF stores AO in R; Unity / BattleTech Standard reads AO from G
        for (int i = 0; i < src.Length; i++) {
          byte ao = src[i].r;
          dst[i] = new Color32(ao, ao, ao, 255);
        }
        var packed = new Texture2D(gltfAo.width, gltfAo.height, TextureFormat.RGBA32, mipChain: true, linear: true) {
          name = name,
          wrapMode = TextureWrapMode.Repeat,
          filterMode = FilterMode.Trilinear
        };
        packed.SetPixels32(dst);
        packed.Apply(updateMipmaps: true, makeNoLongerReadable: true);
        return Track(asset, packed);
      } catch {
        return null;
      }
    }

    private static Texture2D ConvertGltfMetallicRoughnessToUnity(CustomUnityModelAsset asset, Texture2D gltfMr, float metallicFactor, float roughnessFactor, string name) {
      try {
        Color32[] src = gltfMr.GetPixels32();
        var dst = new Color32[src.Length];
        for (int i = 0; i < src.Length; i++) {
          float rough = (src[i].g / 255f) * roughnessFactor;
          float metal = (src[i].b / 255f) * metallicFactor;
          byte mByte = (byte)Mathf.Clamp(Mathf.RoundToInt(metal * 255f), 0, 255);
          byte sByte = (byte)Mathf.Clamp(Mathf.RoundToInt((1f - rough) * 255f), 0, 255);
          dst[i] = new Color32(mByte, mByte, mByte, sByte);
        }
        var packed = new Texture2D(gltfMr.width, gltfMr.height, TextureFormat.RGBA32, mipChain: true, linear: true) {
          name = name,
          wrapMode = TextureWrapMode.Repeat,
          filterMode = FilterMode.Trilinear
        };
        packed.SetPixels32(dst);
        packed.Apply(updateMipmaps: true, makeNoLongerReadable: true);
        return Track(asset, packed);
      } catch {
        return null;
      }
    }

    private static T Track<T>(CustomUnityModelAsset asset, T obj) where T : UnityEngine.Object {
      if (obj != null) asset.OwnedObjects.Add(obj);
      return obj;
    }

    /// <summary>
    /// Drops cached entries so subsequent loads re-read modified files from disk without destroying
    /// Unity Mesh/Material objects still referenced by already-placed props in Designer.
    /// </summary>
    public void InvalidateCacheWithoutDestroying() {
      _models.Clear();
      _proceduralSplits.Clear();
      _autoStumpShells.Clear();
    }

    public void ClearAll() {
      foreach (var asset in _models.Values) {
        if (asset == null) continue;
        foreach (var obj in asset.OwnedObjects) {
          if (obj != null) UnityEngine.Object.Destroy(obj);
        }
        asset.OwnedObjects.Clear();
      }
      _models.Clear();

      foreach (var list in _proceduralSplits.Values) {
        if (list == null) continue;
        foreach (var piece in list) {
          if (piece?.Mesh != null) UnityEngine.Object.Destroy(piece.Mesh);
        }
      }
      _proceduralSplits.Clear();

      foreach (var shell in _autoStumpShells.Values) {
        if (shell != null) UnityEngine.Object.Destroy(shell);
      }
      _autoStumpShells.Clear();
    }
  }
}
