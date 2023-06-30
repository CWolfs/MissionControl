using UnityEngine;

using System.Collections.Generic;

namespace MissionControl.Data {
  public class AssetBundleLoader {
    public static Dictionary<string, AssetBundle> AssetBundles { get; set; } = new Dictionary<string, AssetBundle>(); // General
    public static Dictionary<string, AssetBundle> PropAssetBundles { get; set; } = new Dictionary<string, AssetBundle>(); // Props

    public static bool HasAlreadyLoadedBundle(string bundleName) {
      if (AssetBundles.ContainsKey(bundleName)) return true;
      if (PropAssetBundles.ContainsKey(bundleName)) return true;

      return false;
    }

    public static AssetBundle LoadBundle(string bundleName) {
      AssetBundle bundle = AssetBundle.LoadFromFile(bundleName);
      AssetBundles[bundleName] = bundle;
      return bundle;
    }

    public static AssetBundle LoadPropBundle(string propBundlePath) {
      AssetBundle bundle = AssetBundle.LoadFromFile(propBundlePath);
      PropAssetBundles[propBundlePath] = bundle;
      return bundle;
    }

    public static void UnloadPropBundles() {
      foreach (var propAssetBundlePair in PropAssetBundles) {
        Main.Logger.Log("[AssetBundleLoader.UnloadPropBundles] Unloading prop bundle used in custom contract types: " + propAssetBundlePair.Key);
        propAssetBundlePair.Value.Unload(true);
      }

      PropAssetBundles.Clear();
    }

    public static T GetAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object {
      AssetBundle bundle = null;

      if (AssetBundles.ContainsKey(bundleName)) bundle = AssetBundles[bundleName];

      if (bundle == null) {
        if (PropAssetBundles.ContainsKey(bundleName)) bundle = PropAssetBundles[bundleName];
      }

      if (bundle == null) bundle = LoadBundle(bundleName);

      if (bundle) {
        return (T)bundle.LoadAsset(assetName);
      }

      return null;
    }
  }
}