using System;
using System.IO;
using UnityEngine;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Unity {
  public static class UnityMaterialBuilder {
    private static AssetBundle _fallbackShadersBundle;

    public static Material Build(
      RawMaterialData raw,
      string dataDir,
      Func<AssetRef, Texture2D> resolveTexture,
      Action<string> logWarning = null) {
      if (raw == null) return null;

      Shader shader = ResolveShader(raw.ShaderName, dataDir, logWarning);
      if (shader == null) {
        shader = Shader.Find("BattleTech Standard") ?? Shader.Find("Standard");
      }

      Material mat = new Material(shader) {
        name = raw.Name ?? "VanillaMaterial",
        enableInstancing = raw.EnableInstancing,
        doubleSidedGI = raw.DoubleSidedGI
      };

      if (raw.CustomRenderQueue >= 0) {
        mat.renderQueue = raw.CustomRenderQueue;
      }

      if (raw.ShaderKeywords != null) {
        foreach (string kw in raw.ShaderKeywords) {
          if (!string.IsNullOrEmpty(kw)) {
            mat.EnableKeyword(kw);
          }
        }
      }

      if (raw.StringTagMap != null) {
        foreach (var kv in raw.StringTagMap) {
          mat.SetOverrideTag(kv.Key, kv.Value);
        }
      }

      if (raw.Floats != null) {
        foreach (var kv in raw.Floats) {
          mat.SetFloat(kv.Key, kv.Value);
        }
      }

      if (raw.Colors != null) {
        foreach (var kv in raw.Colors) {
          var c = kv.Value;
          mat.SetColor(kv.Key, new Color(c.X, c.Y, c.Z, c.W));
        }
      }

      if (raw.TexEnvs != null) {
        foreach (var te in raw.TexEnvs) {
          if (!te.TextureRef.IsNull) {
            Texture2D tex = resolveTexture(te.TextureRef);
            if (tex != null) {
              mat.SetTexture(te.PropertyName, tex);
            }
          }
          mat.SetTextureScale(te.PropertyName, new Vector2(te.Scale.X, te.Scale.Y));
          mat.SetTextureOffset(te.PropertyName, new Vector2(te.Offset.X, te.Offset.Y));
        }
      }

      return mat;
    }

    public static Shader ResolveShader(string shaderName, string dataDir, Action<string> logWarning = null) {
      if (string.IsNullOrEmpty(shaderName)) return null;

      Shader shader = Shader.Find(shaderName);
      if (shader != null) return shader;

      // Lazy fallback to BattleTech's own shaders AssetBundle
      try {
        if (_fallbackShadersBundle == null && !string.IsNullOrEmpty(dataDir)) {
          string shadersBundlePath = Path.Combine(dataDir, "StreamingAssets", "data", "assetbundles", "shaders");
          if (File.Exists(shadersBundlePath)) {
            _fallbackShadersBundle = AssetBundle.LoadFromFile(shadersBundlePath);
          }
        }
        if (_fallbackShadersBundle != null) {
          shader = _fallbackShadersBundle.LoadAsset<Shader>(shaderName);
          if (shader != null) return shader;
          // Try matching by shader.name among loaded bundle shaders
          foreach (Shader s in _fallbackShadersBundle.LoadAllAssets<Shader>()) {
            if (s != null && string.Equals(s.name, shaderName, StringComparison.Ordinal)) {
              return s;
            }
          }
        }
      } catch (Exception ex) {
        logWarning?.Invoke($"[BTAssetReader] Failed fallback shader load for '{shaderName}': {ex.Message}");
      }

      logWarning?.Invoke($"[BTAssetReader] Shader '{shaderName}' not found; falling back to BattleTech Standard.");
      return null;
    }

    public static void ReleaseFallbackShaderBundle() {
      if (_fallbackShadersBundle != null) {
        _fallbackShadersBundle.Unload(false);
        _fallbackShadersBundle = null;
      }
    }
  }
}
