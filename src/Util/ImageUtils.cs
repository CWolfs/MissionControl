using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using UnityEngine;

using BattleTech;
using BattleTech.Data;

namespace MissionControl.Utils {
  public static class ImageUtils {
    private static readonly MethodInfo ImageConversionLoadImageMethod = ResolveImageConversionLoadImageMethod();

    private static MethodInfo ResolveImageConversionLoadImageMethod() {
      try {
        Type imageConversionType = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule")
          ?? Type.GetType("UnityEngine.ImageConversion, UnityEngine");
        return imageConversionType?.GetMethod(
          "LoadImage",
          BindingFlags.Public | BindingFlags.Static,
          null,
          new Type[] { typeof(Texture2D), typeof(byte[]) },
          null
        );
      } catch (Exception ex) {
        Main.Logger.LogWarning($"[ImageUtils] Could not resolve UnityEngine.ImageConversion.LoadImage: {ex.Message}");
        return null;
      }
    }

    public static Sprite CreateSprite(Texture2D texture) {
      if (texture == null) return null;
      return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect, Vector4.zero);
    }

    public static Sprite LoadSpriteById(BattleTech.Data.DataManager btDataManager, string spriteId, string dialogueCacheKeyToInvalidate = null) {
      if (btDataManager == null || string.IsNullOrEmpty(spriteId)) return null;

      if (btDataManager.SpriteCache != null && btDataManager.SpriteCache.Contains(spriteId)) {
        Sprite cached = btDataManager.SpriteCache.GetSprite(spriteId);
        if (cached != null) return cached;
      }

      if (btDataManager.TextureManager != null && btDataManager.TextureManager.Contains(spriteId)) {
        Texture2D texture = btDataManager.TextureManager.GetLoadedTexture(spriteId);
        if (texture != null) {
          return DataManager.Instance.GetOrCreateTextureWrapperSprite(texture);
        }
      }

      // Mirror BattleTech's SpriteLoadRequest precedence: check Texture2D manifest entries before Sprite entries.
      if (btDataManager.ResourceLocator != null) {
        VersionManifestEntry textureEntry = btDataManager.ResourceLocator.EntryByID(spriteId, BattleTechResourceType.Texture2D);
        Sprite spriteFromTextureEntry = LoadSpriteFromManifestEntry(btDataManager, BattleTechResourceType.Texture2D, textureEntry, dialogueCacheKeyToInvalidate);
        if (spriteFromTextureEntry != null) return spriteFromTextureEntry;

        VersionManifestEntry spriteEntry = btDataManager.ResourceLocator.EntryByID(spriteId, BattleTechResourceType.Sprite);
        Sprite spriteFromSpriteEntry = LoadSpriteFromManifestEntry(btDataManager, BattleTechResourceType.Sprite, spriteEntry, dialogueCacheKeyToInvalidate);
        if (spriteFromSpriteEntry != null) return spriteFromSpriteEntry;
      }

      return null;
    }

    public static Sprite LoadSpriteFromManifestEntry(
      BattleTech.Data.DataManager btDataManager,
      BattleTechResourceType resourceType,
      VersionManifestEntry entry,
      string dialogueCacheKeyToInvalidate = null
    ) {
      if (entry == null) return null;

      if (entry.IsAssetBundled || entry.IsResourcesAsset) {
        RequestAsyncPortraitResource(btDataManager, resourceType, entry.Id, dialogueCacheKeyToInvalidate);
        return null;
      }

      if (string.IsNullOrEmpty(entry.FilePath)) return null;

      string filePath = entry.FilePath;
      if (!File.Exists(filePath) && !Path.IsPathRooted(filePath)) {
        filePath = Utilities.PathUtils.AppendPath(EmotePortrait.SpriteBasePath, filePath, false);
      }

      return LoadSpriteFromDisk(filePath);
    }

    private static void RequestAsyncPortraitResource(
      BattleTech.Data.DataManager btDataManager,
      BattleTechResourceType resourceType,
      string resourceId,
      string dialogueCacheKeyToInvalidate
    ) {
      if (btDataManager == null || string.IsNullOrEmpty(resourceId)) return;

      string pendingKey = $"{resourceType}:{resourceId}";
      if (!string.IsNullOrEmpty(dialogueCacheKeyToInvalidate)) {
        if (!DataManager.Instance.PendingPortraitCacheKeysByResource.TryGetValue(pendingKey, out HashSet<string> keys)) {
          keys = new HashSet<string>(StringComparer.Ordinal);
          DataManager.Instance.PendingPortraitCacheKeysByResource[pendingKey] = keys;
        }
        keys.Add(dialogueCacheKeyToInvalidate);
      }

      if (!DataManager.Instance.PendingAsyncPortraitLoads.Add(pendingKey)) return;

      LoadRequest loadRequest = btDataManager.CreateLoadRequest(delegate {
        DataManager.Instance.PendingAsyncPortraitLoads.Remove(pendingKey);
        if (DataManager.Instance.PendingPortraitCacheKeysByResource.TryGetValue(pendingKey, out HashSet<string> cacheKeys)) {
          DataManager.Instance.PendingPortraitCacheKeysByResource.Remove(pendingKey);
          foreach (string key in cacheKeys) {
            DialogueContent.dialogueSpriteCache?.Remove(key);
            if (key.EndsWith(".generated", StringComparison.Ordinal)) {
              string pilotKey = key.Substring(0, key.LastIndexOf('.'));
              DataManager.Instance.GeneratedPortraits.Remove(pilotKey);
            }
          }
        }
      });
      loadRequest.AddBlindLoadRequest(resourceType, resourceId, false);
      loadRequest.ProcessRequests();
    }

    public static Sprite LoadSpriteFromDisk(string filePath) {
      if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return null;

      string cacheKey;
      try {
        cacheKey = Path.GetFullPath(filePath);
      } catch (Exception) {
        cacheKey = filePath;
      }

      if (DataManager.Instance.DiskLoadedPortraitSprites.TryGetValue(cacheKey, out Sprite cachedSprite)
        && cachedSprite != null
        && cachedSprite.texture != null) {
        return cachedSprite;
      }

      try {
        byte[] bytes = File.ReadAllBytes(filePath);
        if (bytes == null || bytes.Length < 8) return null;

        Sprite sprite = null;
        if (IsDdsImage(bytes)) {
          Texture2D texture = DecodeDxtTexture(bytes);
          if (texture != null) {
            sprite = CreateSprite(texture);
          }
        } else if (TextureManager.IsPNG(bytes) || TextureManager.IsJPG(bytes)) {
          Texture2D texture = DecodeStandardImageTexture(bytes);
          if (texture != null) {
            sprite = CreateSprite(texture);
          } else if (ImageConversionLoadImageMethod == null) {
            sprite = Utilities.ImageUtils.LoadSprite(filePath);
          }
        } else {
          Main.Logger.LogWarning($"[ImageUtils] Unsupported image format at '{filePath}' (expected DDS, PNG, or JPG).");
          return null;
        }

        if (sprite == null) return null;

        DataManager.Instance.CacheDiskLoadedPortraitSprite(cacheKey, sprite);
        return sprite;
      } catch (Exception ex) {
        Main.Logger.LogWarning($"[ImageUtils] Failed to load sprite from '{filePath}': {ex.Message}");
        return null;
      }
    }

    public static Texture2D DecodeStandardImageTexture(byte[] imageBytes) {
      if (ImageConversionLoadImageMethod == null || imageBytes == null || imageBytes.Length == 0) return null;

      Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, mipChain: false);
      try {
        bool loaded = (bool)ImageConversionLoadImageMethod.Invoke(null, new object[] { texture, imageBytes });
        if (!loaded) {
          UnityEngine.Object.Destroy(texture);
          return null;
        }
        return texture;
      } catch {
        UnityEngine.Object.Destroy(texture);
        throw;
      }
    }

    public static bool IsDdsImage(byte[] bytes) {
      return bytes != null
        && bytes.Length >= 128
        && bytes[0] == 'D' && bytes[1] == 'D' && bytes[2] == 'S' && bytes[3] == ' '
        && BitConverter.ToInt32(bytes, 4) == 124;
    }

    // Decodes DXT1/DXT5 DDS directly from the header dimensions (dwHeight at 12, dwWidth at 16)
    // so non-square images are not distorted and extra mip levels do not break raw upload.
    public static Texture2D DecodeDxtTexture(byte[] ddsBytes) {
      const int headerSize = 128;
      if (!IsDdsImage(ddsBytes)) return null;

      int height = BitConverter.ToInt32(ddsBytes, 12);
      int width = BitConverter.ToInt32(ddsBytes, 16);
      if (width <= 0 || height <= 0 || width > 16384 || height > 16384) {
        return null;
      }

      TextureFormat format;
      int blockSize;
      if (ddsBytes[84] == 'D' && ddsBytes[85] == 'X' && ddsBytes[86] == 'T' && ddsBytes[87] == '1') {
        format = TextureFormat.DXT1;
        blockSize = 8;
      } else if (ddsBytes[84] == 'D' && ddsBytes[85] == 'X' && ddsBytes[86] == 'T' && ddsBytes[87] == '5') {
        format = TextureFormat.DXT5;
        blockSize = 16;
      } else {
        return null;
      }

      long blocksWide = (width + 3L) / 4L;
      long blocksHigh = (height + 3L) / 4L;
      long expectedBytes = blocksWide * blocksHigh * blockSize;
      if (expectedBytes <= 0 || ddsBytes.Length - headerSize < expectedBytes) {
        return null;
      }

      byte[] rawData = new byte[expectedBytes];
      Buffer.BlockCopy(ddsBytes, headerSize, rawData, 0, (int)expectedBytes);

      Texture2D texture = new Texture2D(width, height, format, mipChain: false);
      try {
        texture.LoadRawTextureData(rawData);
        texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
        return texture;
      } catch {
        UnityEngine.Object.Destroy(texture);
        throw;
      }
    }
  }
}
