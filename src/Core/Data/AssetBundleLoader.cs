using UnityEngine;

using System.Collections.Generic;

namespace MissionControl.Data {
  public class AssetBundleLoader {
    public static Dictionary<string, AssetBundle> AssetBundles { get; set; } = new Dictionary<string, AssetBundle>(); // General
    public static Dictionary<string, AssetBundle> PropAssetBundles { get; set; } = new Dictionary<string, AssetBundle>(); // Props

    public static bool HasAlreadyLoadedBundle(string bundleName) {
      if (string.IsNullOrEmpty(bundleName)) return false;
      if (AssetBundles.TryGetValue(bundleName, out AssetBundle generalBundle) && generalBundle != null) return true;
      if (PropAssetBundles.TryGetValue(bundleName, out AssetBundle propBundle) && propBundle != null) return true;

      return false;
    }

    public static AssetBundle LoadBundle(string bundleName) {
      if (string.IsNullOrEmpty(bundleName)) return null;

      AssetBundle bundle = AssetBundle.LoadFromFile(bundleName);
      if (bundle != null) {
        AssetBundles[bundleName] = bundle;
      } else {
        Main.Logger.LogError($"[AssetBundleLoader.LoadBundle] Failed to load bundle '{bundleName}'");
      }
      return bundle;
    }

    public static AssetBundle LoadPropBundle(string propBundlePath) {
      if (string.IsNullOrEmpty(propBundlePath)) return null;

      AssetBundle bundle = AssetBundle.LoadFromFile(propBundlePath);
      if (bundle != null) {
        PropAssetBundles[propBundlePath] = bundle;
      } else {
        Main.Logger.LogError($"[AssetBundleLoader.LoadPropBundle] Failed to load prop bundle '{propBundlePath}'");
      }
      return bundle;
    }

    public static void UnloadPropBundles() {
      foreach (var propAssetBundlePair in PropAssetBundles) {
        if (propAssetBundlePair.Value != null) {
          Main.Logger.Log("[AssetBundleLoader.UnloadPropBundles] Unloading prop bundle used in custom contract types: " + propAssetBundlePair.Key);
          propAssetBundlePair.Value.Unload(true);
        }
      }

      PropAssetBundles.Clear();
    }

    public static T GetAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object {
      if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetName)) return null;

      AssetBundle bundle = null;

      if (AssetBundles.TryGetValue(bundleName, out AssetBundle generalBundle)) {
        if (generalBundle == null) {
          Main.Logger.LogError($"[AssetBundleLoader.GetAsset] General bundle '{bundleName}' was registered as null; cannot load '{assetName}'");
          return null;
        }
        bundle = generalBundle;
      }

      if (bundle == null && PropAssetBundles.TryGetValue(bundleName, out AssetBundle propBundle)) {
        bundle = propBundle;
      }

      if (bundle == null) {
        bundle = LoadPropBundle(bundleName);
      }

      if (bundle != null) {
        return bundle.LoadAsset<T>(assetName);
      }

      return null;
    }
  }
}