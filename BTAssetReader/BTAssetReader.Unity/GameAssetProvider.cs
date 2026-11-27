using System;
using System.Collections.Generic;
using UnityEngine;
using BTAssetReader.Core;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Unity {
  public sealed class SplitPieceAsset {
    public string Name;
    public Vector3 LocalPosition;
    public Quaternion LocalRotation;
    public Vector3 LocalScale;
    public Mesh Mesh;
    public Material[] Materials;
    public RawBoxColliderData BoxCollider;
    public Mesh MeshColliderMesh;
    public bool MeshColliderConvex;
    public float Mass;
  }

  /// <summary>
  /// Shared runtime facade for MissionControl and Designer to load vanilla meshes,
  /// materials, textures, destruction shells, and split hierarchies directly from BattleTech_Data.
  /// </summary>
  public sealed class GameAssetProvider {
    private static GameAssetProvider _instance;
    public static GameAssetProvider Instance {
      get {
        if (_instance == null) _instance = new GameAssetProvider();
        return _instance;
      }
    }

    private GameAssetIndex _index;
    private readonly RuntimeAssetCache _cache = new RuntimeAssetCache();
    private string _dataDir;
    private string _cacheFilePath;
    private Action<string> _log;
    private Action<string> _logWarning;

    public bool IsInitialized => _index != null;
    public GameAssetIndex Index => _index;
    public RuntimeAssetCache Cache => _cache;

    /// <summary>
    /// When true (Designer previews), loaded textures are marked unpinned so EvictPreviewCache can reclaim VRAM.
    /// </summary>
    public bool PreviewLoadMode { get; set; } = false;

    public void Initialize(string battleTechDataDir, string indexCacheFilePath = null, Action<string> log = null, Action<string> logWarning = null) {
      _dataDir = battleTechDataDir;
      _cacheFilePath = indexCacheFilePath;
      _log = log;
      _logWarning = logWarning ?? log;
      EnsureIndex();
    }

    private void EnsureIndex() {
      if (_index != null) return;
      string dataDir = string.IsNullOrEmpty(_dataDir) ? Application.dataPath : _dataDir;
      _dataDir = dataDir;
      _index = GameAssetIndex.BuildOrLoad(dataDir, _cacheFilePath, _log);
    }

    public Mesh TryLoadMesh(string meshName, bool makeReadable = true, MeshPivotMode pivotMode = MeshPivotMode.Original, string sourceAssetFile = null, long pathId = 0) {
      if (string.IsNullOrEmpty(meshName) && pathId == 0) return null;
      EnsureIndex();

      AssetRef aref = _index.FindMesh(meshName, sourceAssetFile, pathId);
      if (aref.IsNull) return null;

      return LoadMeshByRef(aref, makeReadable, pivotMode);
    }

    public Mesh LoadMeshByRef(AssetRef aref, bool makeReadable = true, MeshPivotMode pivotMode = MeshPivotMode.Original) {
      if (aref.IsNull) return null;
      EnsureIndex();

      if (_cache.TryGetMesh(aref, makeReadable, pivotMode, out Mesh cached)) {
        return cached;
      }

      try {
        RawMeshData raw = _index.DecodeMesh(aref);
        if (raw == null) return null;

        Mesh mesh = UnityMeshBuilder.Build(raw, makeReadable, pivotMode);
        _cache.PutMesh(aref, makeReadable, pivotMode, mesh);
        return mesh;
      } catch (Exception ex) {
        _logWarning?.Invoke($"[BTAssetReader] Failed to load Mesh {aref}: {ex.Message}");
        return null;
      }
    }

    public Texture2D TryLoadTexture(string textureName, string sourceAssetFile = null) {
      if (string.IsNullOrEmpty(textureName)) return null;
      EnsureIndex();

      AssetRef aref = _index.FindTexture(textureName, sourceAssetFile);
      if (aref.IsNull) return null;

      return LoadTextureByRef(aref);
    }

    public Texture2D LoadTextureByRef(AssetRef aref) {
      if (aref.IsNull) return null;
      EnsureIndex();

      bool pin = !PreviewLoadMode;
      if (_cache.TryGetTexture(aref, pin, out Texture2D cached)) {
        return cached;
      }

      try {
        RawTexture2DData raw = _index.DecodeTexture2D(aref);
        if (raw == null) return null;

        Texture2D tex = UnityTextureBuilder.Build(raw, _logWarning);
        _cache.PutTexture(aref, tex, pin);
        return tex;
      } catch (Exception ex) {
        _logWarning?.Invoke($"[BTAssetReader] Failed to load Texture2D {aref}: {ex.Message}");
        return null;
      }
    }

    public Material TryLoadMaterial(string materialName, string sourceAssetFile = null) {
      if (string.IsNullOrEmpty(materialName)) return null;
      EnsureIndex();

      AssetRef aref = _index.FindMaterial(materialName, sourceAssetFile);
      if (aref.IsNull) return null;

      return LoadMaterialByRef(aref);
    }

    public Material LoadMaterialByRef(AssetRef aref) {
      if (aref.IsNull) return null;
      EnsureIndex();

      if (_cache.TryGetMaterial(aref, out Material cached)) {
        return cached;
      }

      try {
        RawMaterialData raw = _index.DecodeMaterial(aref);
        if (raw == null) return null;

        Material mat = UnityMaterialBuilder.Build(raw, _dataDir, LoadTextureByRef, _logWarning);
        _cache.PutMaterial(aref, mat);
        return mat;
      } catch (Exception ex) {
        _logWarning?.Invoke($"[BTAssetReader] Failed to load Material {aref}: {ex.Message}");
        return null;
      }
    }

    /// <summary>
    /// Resolves a vanilla destructible split prefab (e.g. "envPrfDeco_smallMilitaryBldgA_generic_static_destruct_split"
    /// or "{meshName}_generic_static_destruct_split") into ready-to-instantiate chunk pieces with exact vanilla per-piece materials.
    /// </summary>
    public List<SplitPieceAsset> TryLoadSplitPrefab(string splitPrefabName, string meshBaseName, string sourceAssetFile = null, Func<string, Material> sceneMaterialLookup = null) {
      EnsureIndex();

      AssetRef rootRef = default;
      if (!string.IsNullOrEmpty(splitPrefabName)) {
        rootRef = _index.FindGameObject(splitPrefabName, sourceAssetFile);
      }

      if (rootRef.IsNull && !string.IsNullOrEmpty(meshBaseName)) {
        // Common vanilla naming conventions in sharedassets*.assets
        string[] candidates = new[] {
          $"{meshBaseName}_generic_static_destruct_split",
          $"envPrfDeco_{meshBaseName}_generic_static_destruct_split",
          $"envPrfComp_{meshBaseName}_generic_static_destruct_split",
          $"envPrfBldn_{meshBaseName}_generic_static_destruct_split",
          $"envPrfStct_{meshBaseName}_generic_static_destruct_split",
          $"{meshBaseName}_split"
        };
        foreach (string c in candidates) {
          rootRef = _index.FindGameObject(c, sourceAssetFile);
          if (!rootRef.IsNull) break;
        }

        if (rootRef.IsNull) {
          string suffix = $"_{meshBaseName}_generic_static_destruct_split";
          foreach (var kv in _index.GameObjectsByName) {
            if (kv.Key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) {
              rootRef = _index.FindGameObject(kv.Key, sourceAssetFile);
              if (!rootRef.IsNull) break;
            }
          }
        }
      }

      if (rootRef.IsNull) return null;

      RawGameObjectNode root = _index.DecodeHierarchy(rootRef);
      if (root == null) return null;

      var pieces = new List<SplitPieceAsset>();
      CollectSplitPieces(root, pieces, sceneMaterialLookup);
      return pieces.Count > 0 ? pieces : null;
    }

    private void CollectSplitPieces(RawGameObjectNode node, List<SplitPieceAsset> pieces, Func<string, Material> sceneMaterialLookup) {
      if (!node.MeshRef.IsNull) {
        Mesh pieceMesh = LoadMeshByRef(node.MeshRef, makeReadable: false, pivotMode: MeshPivotMode.Original);
        if (pieceMesh != null) {
          var mats = new Material[node.MaterialRefs.Count];
          for (int i = 0; i < node.MaterialRefs.Count; i++) {
            string matName = i < node.MaterialNames.Count ? node.MaterialNames[i] : null;
            Material m = !string.IsNullOrEmpty(matName) && sceneMaterialLookup != null ? sceneMaterialLookup(matName) : null;
            if (m == null && !node.MaterialRefs[i].IsNull) {
              m = LoadMaterialByRef(node.MaterialRefs[i]);
            }
            mats[i] = m;
          }

          Mesh colMesh = null;
          bool colConvex = false;
          if (node.MeshCollider != null && !node.MeshCollider.MeshRef.IsNull) {
            colMesh = LoadMeshByRef(node.MeshCollider.MeshRef, makeReadable: false, pivotMode: MeshPivotMode.Original);
            colConvex = node.MeshCollider.Convex;
          }

          pieces.Add(new SplitPieceAsset {
            Name = node.Name,
            LocalPosition = new Vector3(node.LocalPosition.X, node.LocalPosition.Y, node.LocalPosition.Z),
            LocalRotation = new Quaternion(node.LocalRotation.X, node.LocalRotation.Y, node.LocalRotation.Z, node.LocalRotation.W),
            LocalScale = new Vector3(node.LocalScale.X, node.LocalScale.Y, node.LocalScale.Z),
            Mesh = pieceMesh,
            Materials = mats,
            BoxCollider = node.BoxCollider,
            MeshColliderMesh = colMesh,
            MeshColliderConvex = colConvex,
            Mass = node.Rigidbody != null && node.Rigidbody.Mass > 0f ? node.Rigidbody.Mass : 5000f
          });
        }
      }

      if (node.Children != null) {
        foreach (var child in node.Children) {
          CollectSplitPieces(child, pieces, sceneMaterialLookup);
        }
      }
    }

    public int EvictPreviewCache(int maxUnpinnedTextures = 48) {
      return _cache.EvictUnpinnedTextures(maxUnpinnedTextures);
    }

    public void UnloadAll() {
      _cache.UnloadAll();
      UnityMaterialBuilder.ReleaseFallbackShaderBundle();
    }
  }
}
