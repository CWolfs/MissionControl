using Harmony;

using UnityEngine;

using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using BattleTech;
using BattleTech.Assetbundles;
using BattleTech.ModSupport;

namespace MissionControl.Patches {
  [HarmonyPatch()]
  public class AssetBundleManagerGetAssetFromBundlePatch {
    private static string ASSETBUNDLES_PATH => Path.Combine(Application.streamingAssetsPath, "data/assetbundles");
    private static Dictionary<string, GameObject> lookup = new Dictionary<string, GameObject>();

    public static MethodBase TargetMethod() {
      return AccessTools.Method(typeof(AssetBundleManager), "GetAssetFromBundle").MakeGenericMethod(typeof(GameObject));
    }

    public static void Postfix(AssetBundleManager __instance, string assetName, string bundleName, ref GameObject __result) {
      if (__result == null) {
        Main.LogDebug($"[AssetBundleManagerGetAssetFromBundlePatch Postfix] Final stage of trying to load an asset bundle. Attempted to recovery before critical failure.");
        if (lookup.ContainsKey(assetName)) {
          Main.LogDebug($"[AssetBundleManagerGetAssetFromBundlePatch Postfix] Using cached GameObject for '{bundleName}.{assetName}'");
          __result = lookup[assetName];
          return;
        }

        AssetBundle bundle = LoadAssetBundle(bundleName);
        if (bundle != null) {
          Main.LogDebug($"[AssetBundleManagerGetAssetFromBundlePatch Postfix] Force loaded bundle '{bundleName}'");
          AddToLoadedBundles(assetName, bundleName, bundle);
          GameObject asset = GetAssetFromBundle(assetName, bundle);
          lookup[assetName] = asset;
          __result = asset;
        } else {
          Main.LogDebug($"[AssetBundleManagerGetAssetFromBundlePatch Postfix] Bundle is null for '{bundleName}'");
        }
      }
    }

    public static AssetBundle LoadAssetBundle(string assetBundleName) {
      string assetBundlePath = AssetBundleNameToFilepath(assetBundleName);
      return AssetBundle.LoadFromFile(assetBundlePath); // Purposely synchronous
    }

    public static void AddToLoadedBundles(string assetName, string assetBundleName, AssetBundle assetBundle) {
      AssetBundleManager assetBundleManager = (AssetBundleManager)Traverse.Create(UnityGameInstance.BattleTechGame.DataManager).Property("AssetBundleManager").GetValue();
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

    private static string AssetBundleNameToFilepath(string assetBundleName) {
      if (ModLoader.AreModsEnabled && ModLoader.ModAssetBundlePaths.ContainsKey(assetBundleName)) {
        assetBundleName = ModLoader.ModAssetBundlePaths[assetBundleName];
      }
      return Path.Combine(ASSETBUNDLES_PATH, assetBundleName);
    }

    public static void ClearLookup() {
      lookup.Clear();
    }
  }
}