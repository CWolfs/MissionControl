using System;
using System.Collections.Generic;
using System.IO;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Core.CustomModels {
  public static class CustomModelLoader {
    public static bool IsSupportedExtension(string path) {
      if (string.IsNullOrEmpty(path)) return false;
      string ext = Path.GetExtension(path).ToLowerInvariant();
      return ext == ".glb" || ext == ".gltf" || ext == ".obj";
    }

    public static string FindModelFileInDirectory(string directory, string preferredFileName = null) {
      if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return null;

      if (!string.IsNullOrEmpty(preferredFileName)) {
        string direct = Path.IsPathRooted(preferredFileName)
          ? preferredFileName
          : Path.Combine(directory, preferredFileName);
        if (File.Exists(direct)) return direct;
      }

      foreach (string pattern in new[] { "*.glb", "*.gltf", "*.obj" }) {
        string[] found = Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly);
        if (found.Length > 0) {
          Array.Sort(found, StringComparer.OrdinalIgnoreCase);
          return found[0];
        }
      }

      return null;
    }

    public static CustomModelBundle Load(string filePath) {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
        throw new FileNotFoundException("Custom model file not found.", filePath);
      }

      string ext = Path.GetExtension(filePath).ToLowerInvariant();
      CustomModelBundle bundle = ext switch {
        ".glb" or ".gltf" => GltfLoader.Load(filePath),
        ".obj" => ObjLoader.Load(filePath),
        _ => throw new NotSupportedException($"Unsupported custom model format '{ext}'. Supported formats: .glb, .gltf, .obj.")
      };
      ApplyLooseTextureFallbacks(bundle, filePath);
      return bundle;
    }

    private static readonly string[] TextureSubfolderNames = { "textures", "Textures", "maps", "Maps", "images", "Images", "materials", "Materials" };
    private static readonly string[] ModelContainerFolderNames = { "source", "model", "models", "mesh", "meshes", "gltf", "glb", "obj" };
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".webp" };

    /// <summary>
    /// Finds dedicated texture subfolders associated with a model file (e.g. modelDir/textures or
    /// Sketchfab-style ../textures alongside source/model_extracted/). Never scans generic user folders.
    /// </summary>
    public static List<string> FindLooseTextureDirectories(string modelFilePath) {
      var dirs = new List<string>();
      if (string.IsNullOrWhiteSpace(modelFilePath)) return dirs;
      string baseDir = Path.GetDirectoryName(modelFilePath);
      if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir)) return dirs;

      void AddSubfoldersOf(string root) {
        foreach (string tf in TextureSubfolderNames) {
          string sub = Path.Combine(root, tf);
          if (Directory.Exists(sub) && !dirs.Contains(sub)) {
            dirs.Add(sub);
          }
        }
      }

      AddSubfoldersOf(baseDir);

      // Walk up through nested archive/source folders (e.g. zip/source/Foo_extracted -> zip/textures)
      string cursor = baseDir;
      for (int up = 0; up < 3; up++) {
        string dirName = Path.GetFileName(cursor.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!IsNestedModelContainerName(dirName)) break;
        string parent = Path.GetDirectoryName(cursor.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent)) break;
        AddSubfoldersOf(parent);
        cursor = parent;
      }

      return dirs;
    }

    /// <summary>
    /// Returns loose image files sitting in dedicated texture folders for a model (used when saving to a mod
    /// so Sketchfab-style unreferenced textures/ files are copied alongside the model).
    /// </summary>
    public static List<string> FindLooseTextureFiles(string modelFilePath) {
      var files = new List<string>();
      foreach (string dir in FindLooseTextureDirectories(modelFilePath)) {
        try {
          foreach (string f in Directory.GetFiles(dir)) {
            if (IsImageExtension(Path.GetExtension(f))) files.Add(f);
          }
        } catch { }
      }
      files.Sort(StringComparer.OrdinalIgnoreCase);
      return files;
    }

    /// <summary>
    /// Handles Sketchfab "Original Format" archives and similar exports where the .gltf/.obj inside
    /// source/ has untextured material slots while the artist's textures sit in a sibling textures/ folder.
    /// </summary>
    private static void ApplyLooseTextureFallbacks(CustomModelBundle bundle, string modelFilePath) {
      if (bundle?.Lod0Mesh == null || bundle.Lod0Mesh.Uv0 == null || bundle.Materials == null || bundle.Materials.Count == 0) return;

      bool anyMissingAlbedo = false;
      foreach (var m in bundle.Materials) {
        if (m != null && m.BaseColorImageBytes == null) {
          anyMissingAlbedo = true;
          break;
        }
      }
      if (!anyMissingAlbedo) return;

      List<string> candidates = FindLooseTextureFiles(modelFilePath);
      if (candidates.Count == 0) return;

      var albedos = new List<string>();
      var normals = new List<string>();
      var roughMetals = new List<string>();
      var occlusions = new List<string>();
      var emissives = new List<string>();

      foreach (string file in candidates) {
        string stem = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
        if (ContainsAnyToken(stem, "normal", "nrm", "norm", "bump")) normals.Add(file);
        else if (ContainsAnyToken(stem, "metallicroughness", "roughness", "rough", "metallic", "metal", "orm", "arm", "spec")) roughMetals.Add(file);
        else if (ContainsAnyToken(stem, "ambientocclusion", "occlusion", "_ao", "-ao")) occlusions.Add(file);
        else if (ContainsAnyToken(stem, "emissive", "emission", "glow")) emissives.Add(file);
        else if (ContainsAnyToken(stem, "height", "disp", "opacity", "alpha", "mask")) continue;
        else albedos.Add(file);
      }

      if (bundle.Materials.Count == 1) {
        CustomPbrMaterialData mat = bundle.Materials[0];
        if (mat == null) return;

        if (mat.BaseColorImageBytes == null && albedos.Count > 0) {
          string chosen = albedos.Count == 1 ? albedos[0] : PickBestAlbedoForName(albedos, Path.GetFileNameWithoutExtension(modelFilePath));
          if (chosen != null) {
            mat.BaseColorImageBytes = File.ReadAllBytes(chosen);
            mat.BaseColorImageMime = GuessImageMime(chosen);
            bundle.UsedLooseTextureFallback = true;
            // Reset Blender's default 0.8 untextured grey factor so the bound texture isn't darkened
            if (Math.Abs(mat.BaseColorFactor.X - 0.8f) < 0.02f &&
                Math.Abs(mat.BaseColorFactor.Y - 0.8f) < 0.02f &&
                Math.Abs(mat.BaseColorFactor.Z - 0.8f) < 0.02f) {
              mat.BaseColorFactor = new Float4(1f, 1f, 1f, mat.BaseColorFactor.W);
            }
          }
        }
        if (mat.NormalImageBytes == null && normals.Count == 1) {
          mat.NormalImageBytes = File.ReadAllBytes(normals[0]);
          mat.NormalImageMime = GuessImageMime(normals[0]);
        }
        if (mat.OcclusionImageBytes == null && occlusions.Count == 1) {
          mat.OcclusionImageBytes = File.ReadAllBytes(occlusions[0]);
          mat.OcclusionImageMime = GuessImageMime(occlusions[0]);
        }
        if (mat.EmissiveImageBytes == null && emissives.Count == 1) {
          mat.EmissiveImageBytes = File.ReadAllBytes(emissives[0]);
          mat.EmissiveImageMime = GuessImageMime(emissives[0]);
        }
        return;
      }

      // Multi-material fallback: match untextured slots by material name against loose texture stems
      foreach (CustomPbrMaterialData mat in bundle.Materials) {
        if (mat == null || mat.BaseColorImageBytes != null || string.IsNullOrWhiteSpace(mat.Name)) continue;
        string matNorm = NormalizeNameToken(mat.Name);
        if (matNorm.Length < 3 || matNorm.StartsWith("material") || matNorm == "defaultmaterial") continue;

        string hit = PickBestAlbedoForName(albedos, mat.Name, requireNameMatch: true);
        if (hit != null) {
          mat.BaseColorImageBytes = File.ReadAllBytes(hit);
          mat.BaseColorImageMime = GuessImageMime(hit);
          bundle.UsedLooseTextureFallback = true;
          if (Math.Abs(mat.BaseColorFactor.X - 0.8f) < 0.02f &&
              Math.Abs(mat.BaseColorFactor.Y - 0.8f) < 0.02f &&
              Math.Abs(mat.BaseColorFactor.Z - 0.8f) < 0.02f) {
            mat.BaseColorFactor = new Float4(1f, 1f, 1f, mat.BaseColorFactor.W);
          }
        }
      }
    }

    private static string PickBestAlbedoForName(List<string> albedos, string targetName, bool requireNameMatch = false) {
      string normTarget = NormalizeNameToken(targetName);
      string best = null;
      int bestScore = 0;

      foreach (string file in albedos) {
        string stem = Path.GetFileNameWithoutExtension(file);
        string normStem = NormalizeNameToken(stem);
        int score = 0;
        bool nameMatched = !string.IsNullOrEmpty(normTarget) &&
          (normStem.Contains(normTarget) || normTarget.Contains(normStem));
        if (nameMatched) score += 10;
        if (ContainsAnyToken(stem.ToLowerInvariant(), "basecolor", "albedo", "diffuse", "color", "texture", "tex")) score += 3;

        if (requireNameMatch && !nameMatched) continue;
        if (score > bestScore) {
          bestScore = score;
          best = file;
        }
      }

      return best ?? (!requireNameMatch && albedos.Count > 0 ? albedos[0] : null);
    }

    private static bool IsNestedModelContainerName(string dirName) {
      if (string.IsNullOrWhiteSpace(dirName)) return false;
      if (dirName.EndsWith("_extracted", StringComparison.OrdinalIgnoreCase)) return true;
      foreach (string known in ModelContainerFolderNames) {
        if (string.Equals(dirName, known, StringComparison.OrdinalIgnoreCase)) return true;
      }
      return false;
    }

    private static bool IsImageExtension(string ext) {
      foreach (string e in ImageExtensions) {
        if (string.Equals(ext, e, StringComparison.OrdinalIgnoreCase)) return true;
      }
      return false;
    }

    private static string GuessImageMime(string path) {
      string ext = Path.GetExtension(path).ToLowerInvariant();
      return (ext == ".jpg" || ext == ".jpeg") ? "image/jpeg" : (ext == ".webp" ? "image/webp" : "image/png");
    }

    private static bool ContainsAnyToken(string lowerText, params string[] tokens) {
      foreach (string t in tokens) {
        if (lowerText.Contains(t)) return true;
      }
      return false;
    }

    private static string NormalizeNameToken(string s) {
      if (string.IsNullOrEmpty(s)) return string.Empty;
      var chars = new char[s.Length];
      int n = 0;
      for (int i = 0; i < s.Length; i++) {
        char c = s[i];
        if (char.IsLetterOrDigit(c)) chars[n++] = char.ToLowerInvariant(c);
      }
      return new string(chars, 0, n);
    }

    /// <summary>
    /// Resolves an external sidecar file (.bin, .mtl, or texture image) referenced by a .gltf or .obj:
    /// 1. Exact relative (or rooted) path.
    /// 2. Filename fallback across baseDir, common texture subfolders, and up to two parent folders
    ///    (covers Sketchfab nested source/ + root textures/ layouts and artist-baked absolute paths in .mtl).
    /// </summary>
    public static string ResolveExternalAssetPath(string baseDir, string referencePath) {
      if (string.IsNullOrWhiteSpace(referencePath)) return null;

      string cleaned = referencePath.Trim().Trim('"');
      try { cleaned = Uri.UnescapeDataString(cleaned); } catch { }
      string osRel = cleaned.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

      // 1. Exact path (relative to baseDir, or rooted if it exists on this machine)
      if (!string.IsNullOrEmpty(baseDir) && !Path.IsPathRooted(osRel)) {
        string direct = Path.Combine(baseDir, osRel);
        if (File.Exists(direct)) return direct;
      } else if (Path.IsPathRooted(osRel) && File.Exists(osRel)) {
        return osRel;
      }

      if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir)) return null;

      // Extract just the file name (handles foreign Windows/macOS paths like C:\Users\Artist\Desktop\tex.png)
      int lastSep = cleaned.LastIndexOfAny(new[] { '/', '\\' });
      string fileName = lastSep >= 0 ? cleaned.Substring(lastSep + 1) : cleaned;
      if (string.IsNullOrWhiteSpace(fileName)) return null;

      // 2. Search baseDir and its immediate subdirectories first
      string inBase = FindFileCaseInsensitive(baseDir, fileName);
      if (inBase != null) return inBase;

      try {
        foreach (string sub in Directory.GetDirectories(baseDir)) {
          string hit = FindFileCaseInsensitive(sub, fileName);
          if (hit != null) return hit;
        }
      } catch { }

      // 3. Walk up to two parent folders (e.g. Sketchfab zip: root/textures/ vs root/source/model_extracted/)
      string cursor = baseDir;
      for (int up = 0; up < 2; up++) {
        string parent = Path.GetDirectoryName(cursor.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(parent) || !Directory.Exists(parent)) break;

        string inParent = FindFileCaseInsensitive(parent, fileName);
        if (inParent != null) return inParent;

        foreach (string tf in TextureSubfolderNames) {
          string subDir = Path.Combine(parent, tf);
          if (Directory.Exists(subDir)) {
            string hit = FindFileCaseInsensitive(subDir, fileName);
            if (hit != null) return hit;
          }
        }
        cursor = parent;
      }

      return null;
    }

    private static string FindFileCaseInsensitive(string dir, string fileName) {
      if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return null;
      string exact = Path.Combine(dir, fileName);
      if (File.Exists(exact)) return exact;
      try {
        foreach (string f in Directory.GetFiles(dir)) {
          if (string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase)) {
            return f;
          }
        }
      } catch { }
      return null;
    }
  }
}
