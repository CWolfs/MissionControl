using UnityEngine;

using System.Collections.Generic;

namespace MissionControl.Data {
  public class AssetBundleLoader {
    public static Dictionary<string, AssetBundle> AssetBundles { get; set; } = new Dictionary<string, AssetBundle>();

    public static AssetBundle LoadBundle(string bundleName) {
      AssetBundle bundle = AssetBundle.LoadFromFile(bundleName);
      AssetBundles[bundleName] = bundle;
      return bundle;
    }

    public static T GetAsset<T>(string bundleName, string assetName) where T : UnityEngine.Object {
      AssetBundle bundle = null;

      if (AssetBundles.ContainsKey(bundleName)) {
        bundle = AssetBundles[bundleName];
      } else {
        bundle = LoadBundle(bundleName);
      }

      if (bundle) {
        return (T)AssetBundles[bundleName].LoadAsset(assetName);
      }

      return null;
    }
  }
}