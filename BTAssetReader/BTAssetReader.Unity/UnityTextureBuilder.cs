using System;
using UnityEngine;
using BTAssetReader.Core.Models;

namespace BTAssetReader.Unity {
  public static class UnityTextureBuilder {
    public static Texture2D Build(RawTexture2DData raw, Action<string> logWarning = null) {
      if (raw == null || raw.Width <= 0 || raw.Height <= 0 || raw.RawBytes == null || raw.RawBytes.Length == 0) {
        return null;
      }

      TextureFormat format = (TextureFormat)raw.TextureFormat;
      bool mipChain = raw.MipCount > 1;
      bool linear = raw.ColorSpace == 0; // 0 = Linear (normal/metallic/AO), 1 = sRGB (albedo/emission)

      try {
        Texture2D tex = new Texture2D(raw.Width, raw.Height, format, mipChain, linear) {
          name = raw.Name ?? "VanillaTexture",
          filterMode = ToFilterMode(raw.FilterMode),
          anisoLevel = Math.Max(0, Math.Min(16, raw.Aniso)),
          mipMapBias = raw.MipBias,
          wrapModeU = ToWrapMode(raw.WrapU),
          wrapModeV = ToWrapMode(raw.WrapV),
          wrapModeW = ToWrapMode(raw.WrapW)
        };

        tex.LoadRawTextureData(raw.RawBytes);
        tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
        return tex;
      } catch (Exception ex) {
        logWarning?.Invoke($"[BTAssetReader] Failed to upload Texture2D '{raw.Name}' ({raw.Width}x{raw.Height}, fmt={format}, bytes={raw.RawBytes.Length}): {ex.Message}");
        return null;
      }
    }

    private static FilterMode ToFilterMode(int mode) {
      switch (mode) {
        case 0: return FilterMode.Point;
        case 1: return FilterMode.Bilinear;
        case 2: return FilterMode.Trilinear;
        default: return FilterMode.Bilinear;
      }
    }

    private static TextureWrapMode ToWrapMode(int mode) {
      switch (mode) {
        case 0: return TextureWrapMode.Repeat;
        case 1: return TextureWrapMode.Clamp;
        case 2: return TextureWrapMode.Mirror;
        case 3: return TextureWrapMode.MirrorOnce;
        default: return TextureWrapMode.Repeat;
      }
    }
  }
}
