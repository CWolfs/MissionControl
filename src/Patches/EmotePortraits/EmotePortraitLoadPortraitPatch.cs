using System;
using System.IO;

using UnityEngine;

using Harmony;

using BattleTech;

using MissionControl.RuntimeCast;
using MissionControl.Utils;

namespace MissionControl.Patches {
  [HarmonyPatch(typeof(EmotePortrait), "LoadPortrait")]
  public class EmotePortraitLoadPortraitPatch {
    public static bool Prefix(EmotePortrait __instance, ref Sprite __result) {
      if (__instance == null || string.IsNullOrEmpty(__instance.portraitAssetPath)) return true;

      string assetPath = __instance.portraitAssetPath;

      if (assetPath.EndsWith(".generated", StringComparison.Ordinal)) {
        string pilotDefId = assetPath.Substring(0, assetPath.LastIndexOf('.'));

        if (DataManager.Instance.GeneratedPortraits.TryGetValue(pilotDefId, out Sprite cachedSprite) && cachedSprite != null) {
          DataManager.Instance.UnresolvedGeneratedPortraits.Remove(assetPath);
          __result = cachedSprite;
          return false;
        }

        // Lazy fallback in case the portrait was cleared between contracts, restored from a save,
        // or the pilot's sprite finished loading after CreateCast ran.
        PilotDef pilotDef = RuntimeCastFactory.FindPilotDefByKey(pilotDefId);
        if (pilotDef != null) {
          Sprite resolvedSprite = RuntimeCastFactory.CachePilotPortrait(pilotDef);
          if (resolvedSprite != null) {
            __result = resolvedSprite;
            return false;
          }
        }

        // Mark unresolved so DialogueContent.GetPortraitSprite retries once the pilot's sprite finishes loading,
        // and reuse a single cached missing-portrait placeholder instead of re-reading it from disk each time.
        DataManager.Instance.UnresolvedGeneratedPortraits.Add(assetPath);
        Main.LogDebug($"[EmotePortraitLoadPortraitPatch] Issue finding cached generated portrait for '{pilotDefId}'. Using missing portrait placeholder.");
        string missingPath = Utilities.PathUtils.AppendPath(EmotePortrait.SpriteBasePath, EmotePortrait.relativeMissingPortraitPath, false);
        Sprite missingSprite = ImageUtils.LoadSpriteFromDisk(missingPath);
        if (missingSprite != null) {
          __result = missingSprite;
          return false;
        }
        return true;
      }

      // Portraits shipped in MissionControl or in any mod with an 'mcData' folder (PNG/JPG/DDS)
      string modPath = DataManager.Instance.ResolveModFilePath(assetPath);
      if (modPath != null) {
        Sprite modSprite = ImageUtils.LoadSpriteFromDisk(modPath);
        if (modSprite != null) {
          __result = modSprite;
          return false;
        }
      }

      // Let vanilla handle standard PNG/JPG files that exist in StreamingAssets,
      // decode an existing StreamingAssets DDS directly before falling back to resource IDs,
      // and resolve missing StreamingAssets paths via DataManager / ResourceLocator.
      string vanillaPath = Utilities.PathUtils.AppendPath(EmotePortrait.SpriteBasePath, assetPath, false);
      bool vanillaExists = File.Exists(vanillaPath);
      bool isDds = assetPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase);

      if (vanillaExists && !isDds) return true;

      if (vanillaExists && isDds) {
        Sprite ddsSprite = ImageUtils.LoadSpriteFromDisk(vanillaPath);
        if (ddsSprite != null) {
          __result = ddsSprite;
          return false;
        }
      }

      string spriteId = Path.GetFileNameWithoutExtension(assetPath);
      BattleTech.Data.DataManager btDataManager = UnityGameInstance.Instance?.Game?.DataManager;
      Sprite resourceSprite = ImageUtils.LoadSpriteById(btDataManager, spriteId, assetPath);
      if (resourceSprite != null) {
        __result = resourceSprite;
        return false;
      }

      return true;
    }
  }

  [HarmonyPatch(typeof(DialogueContent), "GetPortraitSprite")]
  public static class DialogueContentGetPortraitSpritePatch {
    public static void Prefix(DialogueContent __instance) {
      if (__instance?.castDef == null || DataManager.Instance.UnresolvedGeneratedPortraits.Count == 0) return;

      EmotePortrait emotePortrait = __instance.castDef.GetEmotePortait(__instance.emote);
      string assetPath = emotePortrait?.portraitAssetPath;
      if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".generated", StringComparison.Ordinal)) return;

      // Only retry here when DialogueContent.dialogueSpriteCache holds a cached placeholder that would
      // otherwise bypass EmotePortrait.LoadPortrait; uncached misses go straight to LoadPortrait.
      if (DialogueContent.dialogueSpriteCache == null || !DialogueContent.dialogueSpriteCache.ContainsKey(assetPath)) return;
      if (!DataManager.Instance.UnresolvedGeneratedPortraits.Contains(assetPath)) return;

      string pilotDefId = assetPath.Substring(0, assetPath.LastIndexOf('.'));
      PilotDef pilotDef = RuntimeCastFactory.FindPilotDefByKey(pilotDefId);
      if (pilotDef == null) return;

      Sprite resolvedSprite = RuntimeCastFactory.CachePilotPortrait(pilotDef);
      if (resolvedSprite != null) {
        DialogueContent.dialogueSpriteCache[assetPath] = resolvedSprite;
      }
    }
  }
}