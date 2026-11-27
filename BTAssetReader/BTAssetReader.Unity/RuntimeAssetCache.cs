using System;
using System.Collections.Generic;
using UnityEngine;
using BTAssetReader.Core;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Unity {
  /// <summary>
  /// Tracks Unity Objects reconstructed from BattleTech_Data so they are shared across
  /// props and cleanly destroyed on combat teardown or Designer map switches.
  /// </summary>
  public sealed class RuntimeAssetCache {
    private struct MeshCacheKey : IEquatable<MeshCacheKey> {
      public readonly AssetRef Ref;
      public readonly bool Readable;
      public readonly MeshPivotMode PivotMode;

      public MeshCacheKey(AssetRef aref, bool readable, MeshPivotMode pivotMode) {
        Ref = aref;
        Readable = readable;
        PivotMode = pivotMode;
      }

      public bool Equals(MeshCacheKey other) => Ref.Equals(other.Ref) && Readable == other.Readable && PivotMode == other.PivotMode;
      public override bool Equals(object obj) => obj is MeshCacheKey other && Equals(other);
      public override int GetHashCode() {
        unchecked {
          int h = Ref.GetHashCode();
          h = (h * 397) ^ Readable.GetHashCode();
          h = (h * 397) ^ (int)PivotMode;
          return h;
        }
      }
    }

    private readonly Dictionary<MeshCacheKey, Mesh> _meshes = new Dictionary<MeshCacheKey, Mesh>();
    private readonly Dictionary<AssetRef, Texture2D> _textures = new Dictionary<AssetRef, Texture2D>();
    private readonly Dictionary<AssetRef, Material> _materials = new Dictionary<AssetRef, Material>();
    private readonly LinkedList<AssetRef> _textureLru = new LinkedList<AssetRef>();
    private readonly Dictionary<AssetRef, LinkedListNode<AssetRef>> _textureLruNodes = new Dictionary<AssetRef, LinkedListNode<AssetRef>>();
    private readonly HashSet<AssetRef> _pinnedTextures = new HashSet<AssetRef>();

    public int CachedMeshCount => _meshes.Count;
    public int CachedTextureCount => _textures.Count;
    public int CachedMaterialCount => _materials.Count;

    public bool TryGetMesh(AssetRef aref, bool readable, MeshPivotMode pivotMode, out Mesh mesh) {
      if (_meshes.TryGetValue(new MeshCacheKey(aref, readable, pivotMode), out mesh) && mesh != null) {
        return true;
      }
      mesh = null;
      return false;
    }

    public void PutMesh(AssetRef aref, bool readable, MeshPivotMode pivotMode, Mesh mesh) {
      if (mesh != null) {
        _meshes[new MeshCacheKey(aref, readable, pivotMode)] = mesh;
      }
    }

    public bool TryGetTexture(AssetRef aref, bool pin, out Texture2D tex) {
      if (_textures.TryGetValue(aref, out tex) && tex != null) {
        TouchTexture(aref, pin);
        return true;
      }
      tex = null;
      return false;
    }

    public void PutTexture(AssetRef aref, Texture2D tex, bool pin) {
      if (tex == null) return;
      _textures[aref] = tex;
      TouchTexture(aref, pin);
    }

    public bool TryGetMaterial(AssetRef aref, out Material mat) {
      if (_materials.TryGetValue(aref, out mat) && mat != null) {
        return true;
      }
      mat = null;
      return false;
    }

    public void PutMaterial(AssetRef aref, Material mat) {
      if (mat != null) {
        _materials[aref] = mat;
      }
    }

    private void TouchTexture(AssetRef aref, bool pin) {
      if (pin) _pinnedTextures.Add(aref);
      if (_textureLruNodes.TryGetValue(aref, out var node)) {
        _textureLru.Remove(node);
        _textureLru.AddLast(node);
      } else {
        var newNode = _textureLru.AddLast(aref);
        _textureLruNodes[aref] = newNode;
      }
    }

    /// <summary>
    /// Evicts unpinned (preview-only) textures and materials referencing them when cache exceeds budget.
    /// Useful in Designer during large thumbnail browsing sessions.
    /// </summary>
    public int EvictUnpinnedTextures(int maxUnpinnedTextures) {
      int unpinnedCount = _textures.Count - _pinnedTextures.Count;
      if (unpinnedCount <= maxUnpinnedTextures) return 0;

      int evicted = 0;
      var node = _textureLru.First;
      while (node != null && unpinnedCount > maxUnpinnedTextures) {
        var next = node.Next;
        AssetRef aref = node.Value;
        if (!_pinnedTextures.Contains(aref)) {
          if (_textures.TryGetValue(aref, out Texture2D tex) && tex != null) {
            UnityEngine.Object.Destroy(tex);
          }
          _textures.Remove(aref);
          _textureLru.Remove(node);
          _textureLruNodes.Remove(aref);
          unpinnedCount--;
          evicted++;
        }
        node = next;
      }

      // Drop cached materials whose textures may have been evicted so they recreate cleanly if needed again
      if (evicted > 0) {
        var staleMats = new List<AssetRef>();
        foreach (var kv in _materials) {
          if (kv.Value != null) {
            UnityEngine.Object.Destroy(kv.Value);
          }
          staleMats.Add(kv.Key);
        }
        foreach (var mref in staleMats) _materials.Remove(mref);
      }

      return evicted;
    }

    public void UnloadAll() {
      foreach (var kv in _meshes) {
        if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value);
      }
      _meshes.Clear();

      foreach (var kv in _materials) {
        if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value);
      }
      _materials.Clear();

      foreach (var kv in _textures) {
        if (kv.Value != null) UnityEngine.Object.Destroy(kv.Value);
      }
      _textures.Clear();

      _textureLru.Clear();
      _textureLruNodes.Clear();
      _pinnedTextures.Clear();
    }
  }
}
