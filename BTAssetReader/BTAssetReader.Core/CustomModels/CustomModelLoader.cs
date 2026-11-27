using System;
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
      return ext switch {
        ".glb" or ".gltf" => GltfLoader.Load(filePath),
        ".obj" => ObjLoader.Load(filePath),
        _ => throw new NotSupportedException($"Unsupported custom model format '{ext}'. Supported formats: .glb, .gltf, .obj.")
      };
    }
  }
}
