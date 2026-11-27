using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTAssetReader.Core.Decoders;
using BTAssetReader.Core.Geometry;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core {
  /// <summary>
  /// Loads MissionControl / Designer prop models from Unity 2018.4 AssetBundles (AssetSource="Bundle")
  /// or directly from BattleTech_Data via GameAssetIndex (AssetSource="GameData"), producing the same
  /// CustomModelBundle shape used by .glb/.obj models so Studio can preview, inspect, and edit all prop types uniformly.
  /// </summary>
  public static class PropBundleLoader {
    private static readonly byte[] UnityFsMagic = { (byte)'U', (byte)'n', (byte)'i', (byte)'t', (byte)'y', (byte)'F', (byte)'S' };

    public static bool IsUnityFsFile(string filePath) {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
      try {
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
          if (fs.Length < 16) return false;
          byte[] head = new byte[7];
          if (SerializedFileReader.ReadExact(fs, head, 0, 7) != 7) return false;
          for (int i = 0; i < 7; i++) {
            if (head[i] != UnityFsMagic[i]) return false;
          }
          return true;
        }
      } catch {
        return false;
      }
    }

    public static string FindBundleFileInDirectory(string modelDir, string bundleOverride = null, string modPropsDir = null) {
      if (!string.IsNullOrWhiteSpace(bundleOverride)) {
        if (!string.IsNullOrEmpty(modelDir)) {
          string local = Path.Combine(modelDir, bundleOverride);
          if (IsUnityFsFile(local)) return local;
        }
        if (!string.IsNullOrEmpty(modPropsDir)) {
          string modelsRoot = Path.Combine(modPropsDir, "models");
          if (Directory.Exists(modelsRoot)) {
            foreach (string f in Directory.GetFiles(modelsRoot, bundleOverride, SearchOption.AllDirectories)) {
              if (IsUnityFsFile(f)) return f;
            }
          }
          string bundlesDir = Path.Combine(modPropsDir, "bundles");
          if (Directory.Exists(bundlesDir)) {
            string candidate = Path.Combine(bundlesDir, bundleOverride);
            if (IsUnityFsFile(candidate)) return candidate;
          }
          // Also check mod root /bundles/ when modPropsDir is <Mod>/mcData/props
          try {
            var dirInfo = new DirectoryInfo(Path.GetFullPath(modPropsDir).TrimEnd('\\', '/'));
            if (dirInfo.Parent?.Parent != null) {
              string modBundles = Path.Combine(dirInfo.Parent.Parent.FullName, "bundles", bundleOverride);
              if (IsUnityFsFile(modBundles)) return modBundles;
            }
          } catch { }
        }
      }

      if (string.IsNullOrEmpty(modelDir) || !Directory.Exists(modelDir)) return null;
      foreach (string f in Directory.GetFiles(modelDir)) {
        string ext = Path.GetExtension(f).ToLowerInvariant();
        if (ext == ".json" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".glb" ||
            ext == ".gltf" || ext == ".obj" || ext == ".mtl" || ext == ".manifest" || ext == ".meta" || ext == ".txt") {
          continue;
        }
        if (IsUnityFsFile(f)) return f;
      }
      return null;
    }

    public static CustomModelBundle LoadFromBundle(string bundlePath, string meshBaseName, Float3? colOffset = null) {
      if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath)) {
        throw new FileNotFoundException("AssetBundle file not found.", bundlePath);
      }

      using (var bundle = UnityBundleReader.Open(bundlePath)) {
        UnityBundleReader.DirectoryEntry? primaryOpt = bundle.FindPrimarySerializedFile();
        if (!primaryOpt.HasValue) {
          throw new InvalidDataException($"No serialized asset file found inside bundle '{bundlePath}'.");
        }

        using (Stream s = bundle.OpenEntryStream(primaryOpt.Value)) {
          SerializedFileHeader hdr = SerializedFileReader.ReadHeader(s, primaryOpt.Value.Path, 0);
          if (hdr == null) {
            throw new InvalidDataException($"Could not parse serialized header in bundle '{bundlePath}'.");
          }

          var allMeshes = new List<RawMeshData>();
          foreach (var obj in hdr.ObjectList) {
            if (obj.ClassId != UnityClassId.Mesh) continue;
            byte[] bytes = SerializedFileReader.ReadObjectBytes(s, hdr, obj);
            RawMeshData m = MeshDecoder.Decode(
              bytes,
              hdr.BigEndian,
              new AssetRef(0, obj.PathId),
              Path.GetDirectoryName(bundlePath),
              (sp, off, sz) => bundle.ReadStreamResource(sp, off, sz));
            if (m != null && m.Vertices != null && m.Vertices.Length > 0) {
              EnsureMeshDerived(m);
              allMeshes.Add(m);
            }
          }

          if (allMeshes.Count == 0) {
            throw new InvalidDataException($"No Mesh assets found inside bundle '{bundlePath}'.");
          }

          string baseName = string.IsNullOrWhiteSpace(meshBaseName)
            ? Path.GetFileNameWithoutExtension(bundlePath)
            : meshBaseName;

          RawMeshData lod0 = FindMeshByName(allMeshes, $"{baseName}_LOD0")
            ?? FindMeshByName(allMeshes, baseName)
            ?? allMeshes.FirstOrDefault(m => !IsAuxMeshName(m.Name))
            ?? allMeshes[0];

          RawMeshData lod1 = FindMeshByName(allMeshes, $"{baseName}_LOD1") ?? FindMeshBySuffix(allMeshes, "_lod1", lod0);
          RawMeshData lod2 = FindMeshByName(allMeshes, $"{baseName}_LOD2") ?? FindMeshBySuffix(allMeshes, "_lod2", lod0);
          RawMeshData col = FindMeshByName(allMeshes, $"{baseName}_COL") ?? FindMeshBySuffix(allMeshes, "_col", lod0, excludeShellCol: true);
          RawMeshData glass = FindMeshByName(allMeshes, $"{baseName}_glass") ?? FindMeshBySuffix(allMeshes, "_glass", lod0);
          RawMeshData shell = FindMeshByName(allMeshes, $"{baseName}_shell") ?? FindMeshBySuffix(allMeshes, "_shell", lod0);
          RawMeshData shellCol = FindMeshByName(allMeshes, $"{baseName}_shell_COL") ?? FindMeshBySuffix(allMeshes, "_shell_col", lod0);

          if (col != null && colOffset.HasValue) {
            ApplyVertexOffset(col, colOffset.Value);
          }

          var splits = new List<FracturePieceData>();
          foreach (var m in allMeshes) {
            if (m == lod0 || m == lod1 || m == lod2 || m == col || m == glass || m == shell || m == shellCol) continue;
            string low = (m.Name ?? "").ToLowerInvariant();
            if (low.Contains("split") || low.Contains("chunk") || low.Contains("piece")) {
              splits.Add(CreateSplitPiece(m));
            }
          }

          var matNames = new List<string>();
          foreach (var obj in hdr.ObjectList) {
            if (obj.ClassId != UnityClassId.Material) continue;
            try {
              var mat = MaterialDecoder.Decode(
                SerializedFileReader.ReadObjectBytes(s, hdr, obj),
                hdr.BigEndian,
                new AssetRef(0, obj.PathId),
                p => new AssetRef(p.FileId, p.PathId),
                null);
              if (!string.IsNullOrWhiteSpace(mat?.Name)) matNames.Add(mat.Name);
            } catch { }
          }

          int subCount = lod0.SubMeshIndices != null ? Math.Max(1, lod0.SubMeshIndices.Length) : 1;
          var materials = new List<CustomPbrMaterialData>(subCount);
          for (int i = 0; i < subCount; i++) {
            materials.Add(new CustomPbrMaterialData {
              Name = i < matNames.Count ? matNames[i] : $"Submesh {i}",
              BaseColorFactor = new Float4(0.72f, 0.74f, 0.76f, 1f),
              RoughnessFactor = 0.78f,
              MetallicFactor = 0.15f
            });
          }

          return new CustomModelBundle {
            Name = baseName,
            SourceFilePath = bundlePath,
            Lod0Mesh = lod0,
            Lod1Mesh = lod1,
            Lod2Mesh = lod2,
            ColMesh = col,
            GlassMesh = glass,
            ShellMesh = shell,
            ShellColMesh = shellCol,
            CustomSplits = splits,
            Materials = materials
          };
        }
      }
    }

    public static CustomModelBundle LoadFromGameData(
      GameAssetIndex index,
      string meshBaseName,
      string sourceAssetFile = null,
      string splitPrefabName = null,
      Float3? colOffset = null) {
      if (index == null) throw new InvalidOperationException("BattleTech_Data index is not ready.");
      if (string.IsNullOrWhiteSpace(meshBaseName)) throw new ArgumentException("Mesh name is required.", nameof(meshBaseName));

      RawMeshData DecodeByName(string name) {
        AssetRef r = index.FindMesh(name, sourceAssetFile);
        if (r.IsNull && !string.IsNullOrEmpty(sourceAssetFile)) r = index.FindMesh(name);
        if (r.IsNull) return null;
        RawMeshData m = index.DecodeMesh(r);
        if (m != null) EnsureMeshDerived(m);
        return m;
      }

      RawMeshData lod0 = DecodeByName($"{meshBaseName}_LOD0") ?? DecodeByName(meshBaseName);
      if (lod0 == null) {
        throw new FileNotFoundException($"Mesh '{meshBaseName}' (or '{meshBaseName}_LOD0') was not found in BattleTech_Data.");
      }

      RawMeshData lod1 = DecodeByName($"{meshBaseName}_LOD1");
      RawMeshData lod2 = DecodeByName($"{meshBaseName}_LOD2");
      RawMeshData col = DecodeByName($"{meshBaseName}_COL");
      RawMeshData glass = DecodeByName($"{meshBaseName}_glass");
      RawMeshData shell = DecodeByName($"{meshBaseName}_shell");
      RawMeshData shellCol = DecodeByName($"{meshBaseName}_shell_COL");

      if (col != null && colOffset.HasValue) {
        ApplyVertexOffset(col, colOffset.Value);
      }

      var splits = new List<FracturePieceData>();
      if (!string.IsNullOrWhiteSpace(splitPrefabName)) {
        AssetRef splitGoRef = index.FindGameObject(splitPrefabName, sourceAssetFile);
        if (splitGoRef.IsNull && !string.IsNullOrEmpty(sourceAssetFile)) splitGoRef = index.FindGameObject(splitPrefabName);
        if (!splitGoRef.IsNull) {
          RawGameObjectNode root = index.DecodeHierarchy(splitGoRef);
          if (root != null) {
            CollectHierarchySplits(index, root, new Float3(0f, 0f, 0f), splits);
          }
        }
      }

      int subCount = lod0.SubMeshIndices != null ? Math.Max(1, lod0.SubMeshIndices.Length) : 1;
      var materials = new List<CustomPbrMaterialData>(subCount);
      for (int i = 0; i < subCount; i++) {
        materials.Add(new CustomPbrMaterialData {
          Name = $"Submesh {i}",
          BaseColorFactor = new Float4(0.72f, 0.74f, 0.76f, 1f),
          RoughnessFactor = 0.78f,
          MetallicFactor = 0.15f
        });
      }

      return new CustomModelBundle {
        Name = meshBaseName,
        SourceFilePath = sourceAssetFile ?? "",
        Lod0Mesh = lod0,
        Lod1Mesh = lod1,
        Lod2Mesh = lod2,
        ColMesh = col,
        GlassMesh = glass,
        ShellMesh = shell,
        ShellColMesh = shellCol,
        CustomSplits = splits,
        Materials = materials
      };
    }

    private static void CollectHierarchySplits(GameAssetIndex index, RawGameObjectNode node, Float3 parentPos, List<FracturePieceData> splits) {
      Float3 worldPos = new Float3(
        parentPos.X + node.LocalPosition.X,
        parentPos.Y + node.LocalPosition.Y,
        parentPos.Z + node.LocalPosition.Z);

      if (!node.MeshRef.IsNull) {
        RawMeshData m = index.DecodeMesh(node.MeshRef);
        if (m != null && m.Vertices != null && m.Vertices.Length > 0) {
          EnsureMeshDerived(m);
          if (Math.Abs(worldPos.X) > 1e-4f || Math.Abs(worldPos.Y) > 1e-4f || Math.Abs(worldPos.Z) > 1e-4f) {
            ApplyVertexOffset(m, worldPos);
          }
          splits.Add(CreateSplitPiece(m, node.Name));
        }
      }

      if (node.Children != null) {
        foreach (var ch in node.Children) {
          CollectHierarchySplits(index, ch, worldPos, splits);
        }
      }
    }

    private static FracturePieceData CreateSplitPiece(RawMeshData m, string nameOverride = null) {
      Float3 c = m.LocalAabb.Center;
      for (int i = 0; i < m.Vertices.Length; i++) {
        Float3 v = m.Vertices[i];
        m.Vertices[i] = new Float3(v.X - c.X, v.Y - c.Y, v.Z - c.Z);
      }
      MeshNormaliser.RecalculateBounds(m);
      return new FracturePieceData {
        Name = string.IsNullOrWhiteSpace(nameOverride) ? m.Name : nameOverride,
        Centroid = c,
        Mesh = m,
        LocalBounds = m.LocalAabb,
        EstimatedMass = 500f,
        InteriorSubMeshIndex = -1
      };
    }

    private static void EnsureMeshDerived(RawMeshData m) {
      MeshNormaliser.RecalculateNormalsIfMissing(m);
      MeshNormaliser.RecalculateTangentsIfMissing(m);
      MeshNormaliser.RecalculateBounds(m);
    }

    private static void ApplyVertexOffset(RawMeshData m, Float3 offset) {
      if (m?.Vertices == null) return;
      if (Math.Abs(offset.X) < 1e-5f && Math.Abs(offset.Y) < 1e-5f && Math.Abs(offset.Z) < 1e-5f) return;
      for (int i = 0; i < m.Vertices.Length; i++) {
        Float3 v = m.Vertices[i];
        m.Vertices[i] = new Float3(v.X + offset.X, v.Y + offset.Y, v.Z + offset.Z);
      }
      MeshNormaliser.RecalculateBounds(m);
    }

    private static RawMeshData FindMeshByName(List<RawMeshData> list, string name) =>
      list.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

    private static RawMeshData FindMeshBySuffix(List<RawMeshData> list, string suffix, RawMeshData exclude, bool excludeShellCol = false) {
      foreach (var m in list) {
        if (m == exclude || string.IsNullOrEmpty(m.Name)) continue;
        string low = m.Name.ToLowerInvariant();
        if (excludeShellCol && low.EndsWith("_shell_col")) continue;
        if (low.EndsWith(suffix)) return m;
      }
      return null;
    }

    /// <summary>
    /// Resolves and decodes MissionControl's real Rubble shell meshes (smallCivilianBldgG_shell + smallMilitaryBarracksA_shell)
    /// from common-assets-bundle (in Mods/MissionControl/bundles or sibling workspace) with BattleTech_Data fallback.
    /// </summary>
    public static (RawMeshData DebrisMesh, RawMeshData WallsMesh) LoadMissionControlRubbleMeshes(
      string btDataPath,
      string modPropsDir,
      GameAssetIndex btIndex) {
      RawMeshData debris = null;
      RawMeshData walls = null;

      string bundlePath = FindCommonAssetsBundle(btDataPath, modPropsDir);
      if (!string.IsNullOrEmpty(bundlePath)) {
        try {
          using (var bundle = UnityBundleReader.Open(bundlePath)) {
            UnityBundleReader.DirectoryEntry? primaryOpt = bundle.FindPrimarySerializedFile();
            if (primaryOpt.HasValue) {
              using (Stream s = bundle.OpenEntryStream(primaryOpt.Value)) {
                SerializedFileHeader hdr = SerializedFileReader.ReadHeader(s, primaryOpt.Value.Path, 0);
                if (hdr != null) {
                  foreach (var obj in hdr.ObjectList) {
                    if (obj.ClassId != UnityClassId.Mesh) continue;
                    byte[] bytes = SerializedFileReader.ReadObjectBytes(s, hdr, obj);
                    RawMeshData m = MeshDecoder.Decode(
                      bytes,
                      hdr.BigEndian,
                      new AssetRef(0, obj.PathId),
                      Path.GetDirectoryName(bundlePath),
                      (sp, off, sz) => bundle.ReadStreamResource(sp, off, sz));
                    if (m == null || m.Vertices == null || m.Vertices.Length == 0) continue;
                    EnsureMeshDerived(m);
                    string n = m.Name ?? "";
                    if (n.Equals("smallCivilianBldgG_shell", StringComparison.OrdinalIgnoreCase)) debris = m;
                    else if (n.Equals("smallMilitaryBarracksA_shell", StringComparison.OrdinalIgnoreCase)) walls = m;
                  }
                }
              }
            }
          }
        } catch { }
      }

      if (btIndex != null) {
        if (debris == null) {
          AssetRef r = btIndex.FindMesh("smallCivilianBldgG_shell");
          if (!r.IsNull) { debris = btIndex.DecodeMesh(r); EnsureMeshDerived(debris); }
        }
        if (walls == null) {
          AssetRef r = btIndex.FindMesh("smallMilitaryBarracksA_shell");
          if (!r.IsNull) { walls = btIndex.DecodeMesh(r); EnsureMeshDerived(walls); }
        }
      }

      return (debris, walls);
    }

    private static string FindCommonAssetsBundle(string btDataPath, string modPropsDir) {
      var candidates = new List<string>();
      if (!string.IsNullOrWhiteSpace(btDataPath)) {
        try {
          string gameRoot = Path.GetDirectoryName(Path.GetFullPath(btDataPath).TrimEnd('\\', '/'));
          if (!string.IsNullOrEmpty(gameRoot)) {
            candidates.Add(Path.Combine(gameRoot, "Mods", "MissionControl", "bundles", "common-assets-bundle"));
          }
        } catch { }
      }
      if (!string.IsNullOrWhiteSpace(modPropsDir)) {
        try {
          var dir = new DirectoryInfo(Path.GetFullPath(modPropsDir).TrimEnd('\\', '/'));
          for (int i = 0; i < 4 && dir != null; i++) {
            candidates.Add(Path.Combine(dir.FullName, "MissionControl", "bundles", "common-assets-bundle"));
            candidates.Add(Path.Combine(dir.FullName, "bundles", "common-assets-bundle"));
            dir = dir.Parent;
          }
        } catch { }
      }
      foreach (string c in candidates) {
        if (IsUnityFsFile(c)) return c;
      }
      return null;
    }

    private static bool IsAuxMeshName(string name) {
      if (string.IsNullOrEmpty(name)) return false;
      string low = name.ToLowerInvariant();
      return low.EndsWith("_col") || low.EndsWith("_collider") || low.EndsWith("_lod1") || low.EndsWith("_lod2") ||
             low.EndsWith("_glass") || low.EndsWith("_shell") || low.EndsWith("_shell_col") || low.Contains("split");
    }
  }
}
