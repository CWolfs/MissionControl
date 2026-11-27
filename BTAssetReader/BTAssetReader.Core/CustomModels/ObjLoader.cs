using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BTAssetReader.Core.Geometry;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.CustomModels {
  /// <summary>
  /// Pure-C# Wavefront .obj + .mtl loader producing Unity-space (left-handed Y-up) RawMeshData.
  /// </summary>
  public static class ObjLoader {
    private sealed class ObjSlotBuilder {
      public readonly List<Float3> Vertices = new List<Float3>();
      public readonly List<Float3> Normals = new List<Float3>();
      public readonly List<Float2> Uvs = new List<Float2>();
      public readonly List<int> VertexPartIds = new List<int>();
      public readonly List<ModelPartInfo> Parts = new List<ModelPartInfo>();
      private readonly Dictionary<string, int> _partIdByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
      private int _currentPartId = -1;
      public readonly Dictionary<string, int> VertCache = new Dictionary<string, int>(StringComparer.Ordinal);
      public readonly Dictionary<string, List<int>> TrisByMat = new Dictionary<string, List<int>>(StringComparer.Ordinal);
      public readonly List<string> MatOrder = new List<string>();
      public bool HasAnyNormal;
      public bool HasAnyUv;

      public void SetActivePart(string name) {
        string key = string.IsNullOrWhiteSpace(name) ? "default" : name.Trim();
        if (!_partIdByName.TryGetValue(key, out int pid)) {
          pid = Parts.Count;
          _partIdByName[key] = pid;
          Parts.Add(new ModelPartInfo { Name = key });
        }
        _currentPartId = pid;
      }

      public int GetOrAddVertex(int vi, int vti, int vni, List<Float3> srcPos, List<Float2> srcUv, List<Float3> srcNorm) {
        if (_currentPartId < 0) SetActivePart("default");
        string key = $"{_currentPartId}:{vi}/{vti}/{vni}";
        if (VertCache.TryGetValue(key, out int existing)) return existing;

        Float3 p = vi >= 0 && vi < srcPos.Count ? srcPos[vi] : new Float3(0f, 0f, 0f);
        // Right-handed OBJ -> Unity left-handed: negate X
        Vertices.Add(new Float3(-p.X, p.Y, p.Z));
        VertexPartIds.Add(_currentPartId);

        if (vni >= 0 && vni < srcNorm.Count) {
          Float3 n = srcNorm[vni];
          Normals.Add(new Float3(-n.X, n.Y, n.Z));
          HasAnyNormal = true;
        } else {
          Normals.Add(new Float3(0f, 1f, 0f));
        }

        if (vti >= 0 && vti < srcUv.Count) {
          Uvs.Add(srcUv[vti]);
          HasAnyUv = true;
        } else {
          Uvs.Add(new Float2(0f, 0f));
        }

        int idx = Vertices.Count - 1;
        VertCache[key] = idx;
        return idx;
      }

      public void AddTriangle(string matName, int a, int b, int c) {
        if (!TrisByMat.TryGetValue(matName, out var list)) {
          list = new List<int>();
          TrisByMat[matName] = list;
          MatOrder.Add(matName);
        }
        // Converting right-handed CCW OBJ -> left-handed CW Unity via X-negation flips dot(cross, n),
        // so reverse winding (a, c, b) to keep front faces outward in Unity.
        list.Add(a);
        list.Add(c);
        list.Add(b);
        if (_currentPartId >= 0 && _currentPartId < Parts.Count) {
          Parts[_currentPartId].TriangleCount++;
        }
      }

      public RawMeshData ToRawMesh(string name, out List<string> usedMats, List<string> preferredMatOrder = null) {
        usedMats = new List<string>();
        if (Vertices.Count == 0 || MatOrder.Count == 0) return null;

        List<string> orderedMats = preferredMatOrder != null && preferredMatOrder.Count > 0
          ? preferredMatOrder
          : MatOrder;

        int subCount = orderedMats.Count;
        var subMeshes = new RawSubMesh[subCount];
        var subIndices = new int[subCount][];
        for (int s = 0; s < subCount; s++) {
          string mn = orderedMats[s];
          usedMats.Add(mn);
          int[] tris = TrisByMat.TryGetValue(mn, out var list) ? list.ToArray() : Array.Empty<int>();
          subIndices[s] = tris;
          subMeshes[s] = new RawSubMesh {
            Topology = 0,
            IndexCount = (uint)tris.Length
          };
        }

        var mesh = new RawMeshData {
          Name = name,
          VertexCount = Vertices.Count,
          IndexFormat = Vertices.Count > 65535 ? 1 : 0,
          Vertices = Vertices.ToArray(),
          Normals = HasAnyNormal ? Normals.ToArray() : null,
          Uv0 = HasAnyUv ? Uvs.ToArray() : null,
          SubMeshes = subMeshes,
          SubMeshIndices = subIndices,
          OriginalIsReadable = true,
          Parts = Parts.Count > 0 ? Parts : null,
          VertexPartIds = Parts.Count > 0 ? VertexPartIds.ToArray() : null
        };

        MeshNormaliser.RecalculateNormalsIfMissing(mesh);
        MeshNormaliser.RecalculateTangentsIfMissing(mesh);
        MeshNormaliser.RecalculateBounds(mesh);
        MeshNormaliser.RecalculatePartBounds(mesh);
        return mesh;
      }
    }

    public static CustomModelBundle Load(string filePath) {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
        throw new FileNotFoundException("OBJ file not found.", filePath);
      }

      string baseDir = Path.GetDirectoryName(filePath) ?? string.Empty;
      var srcPos = new List<Float3>();
      var srcUv = new List<Float2>();
      var srcNorm = new List<Float3>();
      var mtlMaterials = new Dictionary<string, CustomPbrMaterialData>(StringComparer.OrdinalIgnoreCase);

      var lod0 = new ObjSlotBuilder();
      var lod1 = new ObjSlotBuilder();
      var lod2 = new ObjSlotBuilder();
      var col = new ObjSlotBuilder();
      var glass = new ObjSlotBuilder();
      var shell = new ObjSlotBuilder();
      var shellCol = new ObjSlotBuilder();
      var splitBuilders = new Dictionary<string, ObjSlotBuilder>(StringComparer.OrdinalIgnoreCase);

      string currentGroup = "default";
      string currentMat = "DefaultMaterial";
      ObjSlotBuilder activeBuilder = lod0;

      foreach (string rawLine in File.ReadLines(filePath)) {
        string line = rawLine.Trim();
        if (line.Length == 0 || line[0] == '#') continue;

        int sp = line.IndexOf(' ');
        if (sp < 0) continue;
        string cmd = line.Substring(0, sp);
        string rest = line.Substring(sp + 1).Trim();

        switch (cmd) {
          case "v": {
            string[] p = rest.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length >= 3) {
              srcPos.Add(new Float3(ParseF(p[0]), ParseF(p[1]), ParseF(p[2])));
            }
            break;
          }
          case "vt": {
            string[] p = rest.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length >= 2) {
              srcUv.Add(new Float2(ParseF(p[0]), ParseF(p[1])));
            }
            break;
          }
          case "vn": {
            string[] p = rest.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length >= 3) {
              srcNorm.Add(new Float3(ParseF(p[0]), ParseF(p[1]), ParseF(p[2])));
            }
            break;
          }
          case "mtllib": {
            string mtlPath = CustomModelLoader.ResolveExternalAssetPath(baseDir, rest);
            if (mtlPath != null) {
              ParseMtl(mtlPath, mtlMaterials);
            }
            break;
          }
          case "usemtl": {
            currentMat = string.IsNullOrWhiteSpace(rest) ? "DefaultMaterial" : rest;
            break;
          }
          case "o":
          case "g": {
            currentGroup = string.IsNullOrWhiteSpace(rest) ? "default" : rest;
            string slot = GltfLoader.ClassifySlotName(currentGroup);
            if (slot == "Split") {
              if (!splitBuilders.TryGetValue(currentGroup, out activeBuilder)) {
                activeBuilder = new ObjSlotBuilder();
                splitBuilders[currentGroup] = activeBuilder;
              }
            } else {
              activeBuilder = slot switch {
                "COL" => col,
                "LOD1" => lod1,
                "LOD2" => lod2,
                "Glass" => glass,
                "Shell" => shell,
                "ShellCOL" => shellCol,
                _ => lod0
              };
            }
            activeBuilder.SetActivePart(currentGroup);
            break;
          }
          case "f": {
            string[] tokens = rest.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 3) break;
            int[] faceVerts = new int[tokens.Length];
            for (int i = 0; i < tokens.Length; i++) {
              ParseFaceToken(tokens[i], srcPos.Count, srcUv.Count, srcNorm.Count, out int vi, out int vti, out int vni);
              faceVerts[i] = activeBuilder.GetOrAddVertex(vi, vti, vni, srcPos, srcUv, srcNorm);
            }
            // Fan triangulation for triangles, quads, and convex n-gons
            for (int i = 1; i + 1 < faceVerts.Length; i++) {
              activeBuilder.AddTriangle(currentMat, faceVerts[0], faceVerts[i], faceVerts[i + 1]);
            }
            break;
          }
        }
      }

      string modelName = Path.GetFileNameWithoutExtension(filePath);
      RawMeshData lod0Mesh = lod0.ToRawMesh($"{modelName}_LOD0", out List<string> usedMatNames);
      if (lod0Mesh == null) {
        throw new InvalidDataException($"No renderable triangle geometry found in '{filePath}'.");
      }

      var orderedMats = new List<CustomPbrMaterialData>(usedMatNames.Count);
      foreach (string mn in usedMatNames) {
        if (mtlMaterials.TryGetValue(mn, out var m)) orderedMats.Add(m);
        else orderedMats.Add(new CustomPbrMaterialData { Name = mn });
      }

      var customSplits = new List<FracturePieceData>();
      foreach (var kv in splitBuilders) {
        RawMeshData sm = kv.Value.ToRawMesh(kv.Key, out _);
        if (sm == null) continue;
        Float3 c = sm.LocalAabb.Center;
        for (int i = 0; i < sm.Vertices.Length; i++) {
          sm.Vertices[i] = new Float3(sm.Vertices[i].X - c.X, sm.Vertices[i].Y - c.Y, sm.Vertices[i].Z - c.Z);
        }
        MeshNormaliser.RecalculateBounds(sm);
        customSplits.Add(new FracturePieceData {
          Name = kv.Key,
          Centroid = c,
          Mesh = sm,
          LocalBounds = sm.LocalAabb,
          EstimatedMass = GltfLoader.EstimateMass(sm.LocalAabb)
        });
      }

      return new CustomModelBundle {
        Name = modelName,
        SourceFilePath = filePath,
        Lod0Mesh = lod0Mesh,
        Lod1Mesh = lod1.ToRawMesh($"{modelName}_LOD1", out _, usedMatNames),
        Lod2Mesh = lod2.ToRawMesh($"{modelName}_LOD2", out _, usedMatNames),
        ColMesh = col.ToRawMesh($"{modelName}_COL", out _),
        GlassMesh = glass.ToRawMesh($"{modelName}_glass", out _),
        ShellMesh = shell.ToRawMesh($"{modelName}_shell", out _),
        ShellColMesh = shellCol.ToRawMesh($"{modelName}_shell_COL", out _),
        CustomSplits = customSplits,
        Materials = orderedMats
      };
    }

    private static void ParseMtl(string mtlPath, Dictionary<string, CustomPbrMaterialData> dst) {
      string dir = Path.GetDirectoryName(mtlPath) ?? string.Empty;
      CustomPbrMaterialData cur = null;

      foreach (string raw in File.ReadLines(mtlPath)) {
        string line = raw.Trim();
        if (line.Length == 0 || line[0] == '#') continue;
        int sp = line.IndexOf(' ');
        if (sp < 0) continue;
        string cmd = line.Substring(0, sp);
        string rest = line.Substring(sp + 1).Trim();

        if (cmd.Equals("newmtl", StringComparison.OrdinalIgnoreCase)) {
          cur = new CustomPbrMaterialData { Name = rest };
          dst[rest] = cur;
        } else if (cur != null) {
          switch (cmd.ToLowerInvariant()) {
            case "kd": {
              string[] p = rest.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
              if (p.Length >= 3) cur.BaseColorFactor = new Float4(ParseF(p[0]), ParseF(p[1]), ParseF(p[2]), cur.BaseColorFactor.W);
              break;
            }
            case "d": {
              float alpha = Math.Max(0f, Math.Min(1f, ParseF(rest)));
              cur.BaseColorFactor = new Float4(cur.BaseColorFactor.X, cur.BaseColorFactor.Y, cur.BaseColorFactor.Z, alpha);
              if (alpha < 0.999f) cur.AlphaMode = "BLEND";
              break;
            }
            case "tr": {
              float alpha = Math.Max(0f, Math.Min(1f, 1f - ParseF(rest)));
              cur.BaseColorFactor = new Float4(cur.BaseColorFactor.X, cur.BaseColorFactor.Y, cur.BaseColorFactor.Z, alpha);
              if (alpha < 0.999f) cur.AlphaMode = "BLEND";
              break;
            }
            case "ke": {
              string[] p = rest.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
              if (p.Length >= 3) cur.EmissiveFactor = new Float3(ParseF(p[0]), ParseF(p[1]), ParseF(p[2]));
              break;
            }
            case "pm":
              cur.MetallicFactor = ParseF(rest);
              break;
            case "pr":
              cur.RoughnessFactor = ParseF(rest);
              break;
            case "map_kd":
              cur.BaseColorImageBytes = TryReadMtlTexture(dir, rest, out cur.BaseColorImageMime);
              break;
            case "map_bump":
            case "bump":
              cur.NormalImageBytes = TryReadMtlTexture(dir, rest, out cur.NormalImageMime);
              break;
            case "map_ke":
              cur.EmissiveImageBytes = TryReadMtlTexture(dir, rest, out cur.EmissiveImageMime);
              break;
          }
        }
      }
    }

    private static byte[] TryReadMtlTexture(string dir, string arg, out string mime) {
      mime = "image/png";
      // Strip optional OBJ texture flags (e.g. -bm 1.0 filename.png), preserving spaces in filenames if no flags
      string[] parts = arg.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
      string fileRef = arg;
      if (parts.Length > 1 && parts[0].StartsWith("-")) {
        fileRef = parts[parts.Length - 1];
      }
      string full = CustomModelLoader.ResolveExternalAssetPath(dir, fileRef)
        ?? (parts.Length > 0 ? CustomModelLoader.ResolveExternalAssetPath(dir, parts[parts.Length - 1]) : null);
      if (full == null) return null;
      string ext = Path.GetExtension(full).ToLowerInvariant();
      mime = (ext == ".jpg" || ext == ".jpeg") ? "image/jpeg" : (ext == ".webp" ? "image/webp" : "image/png");
      return File.ReadAllBytes(full);
    }

    private static void ParseFaceToken(string token, int posCount, int uvCount, int normCount, out int vi, out int vti, out int vni) {
      vi = -1; vti = -1; vni = -1;
      string[] parts = token.Split('/');
      if (parts.Length >= 1 && int.TryParse(parts[0], out int pi)) vi = pi > 0 ? pi - 1 : posCount + pi;
      if (parts.Length >= 2 && parts[1].Length > 0 && int.TryParse(parts[1], out int ti)) vti = ti > 0 ? ti - 1 : uvCount + ti;
      if (parts.Length >= 3 && parts[2].Length > 0 && int.TryParse(parts[2], out int ni)) vni = ni > 0 ? ni - 1 : normCount + ni;
    }

    private static float ParseF(string s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f) ? f : 0f;
  }
}
