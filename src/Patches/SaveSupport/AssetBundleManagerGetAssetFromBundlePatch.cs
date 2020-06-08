using Harmony;

using UnityEngine;

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Assetbundles;

namespace MissionControl.Patches {
  [HarmonyPatch()]
  public class AssetBundleManagerGetAssetFromBundlePatch {
    private static Dictionary<string, GameObject> lookup = new Dictionary<string, GameObject>();
    private static List<string> ignoreBundles = new List<string>() {
      "vfx",
      "weaponeffects",
      "shaders"
    };

    public static MethodBase TargetMethod() {
      return AccessTools.Method(typeof(AssetBundleManager), "GetAssetFromBundle").MakeGenericMethod(typeof(GameObject));
    }

    private static bool IsIgnoredBundle(string bundleName) {
      if (ignoreBundles.Contains(bundleName)) return true;
      return false;
    }

    public static void Postfix(AssetBundleManager __instance, string assetName, string bundleName, ref GameObject __result) {
      if (MissionControl.Instance.AllowMissionControl() && !IsIgnoredBundle(bundleName)) {
        if (__result == null) {
          Main.LogDebug($"[AssetBundleManagerGetAssetFromBundlePatch Postfix] Final stage of trying to load an asset bundle. Attempted to recovery before critical failure.");
          if (lookup.ContainsKey(assetName)) {
            Main.LogDebug($"[AssetBundleManagerGetAssetFromBundlePatch Postfix] Using cached GameObject for '{bundleName}.{assetName}'");
            __result = lookup[assetName];
            return;
          }

          AssetBundleManager assetBundleManager = (AssetBundleManager)Traverse.Create(UnityGameInstance.BattleTechGame.DataManager).Property("AssetBundleManager").GetValue();
          AssetBundle bundle = LoadAssetBundle(assetBundleManager, bundleName);

          if (bundle != null) {
            Main.LogDebug($"[AssetBundleManagerGetAssetFromBundlePatch Postfix] Force loaded bundle '{bundleName}'");
            AddToLoadedBundles(assetBundleManager, assetName, bundleName, bundle);
            GameObject asset = GetAssetFromBundle(assetName, bundle);
            if (asset == null) {
              Main.LogDebug($"[AssetBundleManagerGetAssetFromBundlePatch Postfix] Asset couldn't be loaded from bundle. Returning null.");
            }
            lookup[assetName] = asset;
            __result = asset;
          } else {
            Main.LogDebug($"[AssetBundleManagerGetAssetFromBundlePatch Postfix] Bundle is null for '{bundleName}'");
          }
        }
      }
    }

    public static AssetBundle LoadAssetBundle(AssetBundleManager assetBundleManager, string assetBundleName) {
      string assetBundlePath = (string)AccessTools.Method(typeof(AssetBundleManager), "AssetBundleNameToFilepath").Invoke(assetBundleManager, new object[] { assetBundleName });
      return AssetBundle.LoadFromFile(assetBundlePath); // Purposely synchronous
    }

    public static void AddToLoadedBundles(AssetBundleManager assetBundleManager, string assetName, string assetBundleName, AssetBundle assetBundle) {
      Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().First(
        a => (a.FullName.StartsWith("Assembly-CSharp") && !a.FullName.StartsWith("Assembly-CSharp-firstpass"))
      );
      var assetTrackerType = assembly.GetType("BattleTech.Assetbundles.AssetBundleTracker");
      var assetTracker = Activator.CreateInstance(assetTrackerType, new object[] { assetBundle, false });
      Traverse.Create(assetTracker).Property("CurrentState").SetValue(3);

      AssetBundleIORequestOperation hijackedRequest = (AssetBundleIORequestOperation)Activator.CreateInstance(typeof(AssetBundleIORequestOperation), new object[] { "ignore_this_message", assetBundleName, false, (LoadOperationComplete)EmptyOnLoaded, (uint)0u, (uint)0u });
      AccessTools.Property(typeof(AssetBundleLoadOperation), "AssetBundle").SetValue(hijackedRequest, assetBundle);
      AccessTools.Property(typeof(AssetBundleLoadOperation), "Tracker").SetValue(hijackedRequest, assetTracker);

      AccessTools.Method(typeof(AssetBundleManager), "BundleLoaded").Invoke(assetBundleManager, new object[] { hijackedRequest });
    }

    public static void EmptyOnLoaded(AssetBundleLoadOperation operation) { }

    public static GameObject GetAssetFromBundle(string assetName, AssetBundle assetBundle) {
      return assetBundle.LoadAsset<GameObject>(assetName);
    }

    public static void ClearLookup() {
      lookup.Clear();
    }
  }
}