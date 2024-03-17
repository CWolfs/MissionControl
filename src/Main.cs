using UnityEngine;

using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Globalization;

using System.Collections.Generic;

using HBS.Logging;

using Harmony;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Reflection;

using MissionControl.Data;
using MissionControl.Config;
using MissionControl.Utils;
using BattleTech.UI;

namespace MissionControl {
  public class Main {
    public static ILog Logger;
    public static Config.Settings Settings { get; private set; }
    public static Assembly MissionControlAssembly { get; set; }
    public static string Path { get; private set; }

    public static AssetBundle CommonAssetsBundle { get; set; }

    public static void InitLogger(string modDirectory) {
      Dictionary<string, LogLevel> logLevels = new Dictionary<string, LogLevel> {
        ["MissionControl"] = LogLevel.Debug
      };
      LogManager.Setup(modDirectory + "/output.log", logLevels);
      Logger = LogManager.GetLogger("MissionControl");
      Path = modDirectory;
    }

    public static void LogDebug(string message) {
      if (Main.Settings.DebugMode) Main.Logger.LogDebug(message);
    }

    public static void LogDebugWarning(string message) {
      if (Main.Settings.DebugMode) Main.Logger.LogWarning(message);
    }

    public static void LogDeveloperWarning(string message) {
      Main.Logger.LogWarning(message);

      if (Main.Settings.CustomContractTypes.DeveloperMode) {
        GenericPopupBuilder.Create(GenericPopupType.Warning, message).AddButton("OK", null, true, null).Render();
      }
    }

    public static void LoadAssetBundles() {
      CommonAssetsBundle = AssetBundle.LoadFromFile($"{Main.Path}/bundles/common-assets-bundle");
      AssetBundleLoader.AssetBundles.Add("common-assets-bundle", CommonAssetsBundle);
    }

    // Entry point into the mod, specified in the `mod.json`
    public static void Init(string modDirectory, string modSettings) {
      try {
        InitLogger(modDirectory);
        LoadSettings(modDirectory);
        LoadAssetBundles();
        LoadData(modDirectory);
        VersionCheck();
      } catch (Exception e) {
        Logger.LogError(e);
        Logger.Log("Error loading mod settings - using defaults.");
        Settings = new Config.Settings();
      }

      HarmonyInstance harmony = HarmonyInstance.Create("co.uk.cwolf.MissionControl");
      harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    private static void VersionCheck() {
      try {
        string modJson = new WebClient().DownloadString("https://raw.githubusercontent.com/CWolfs/MissionControl/master/mod.json");
        JObject json = JObject.Parse(modJson);
        string version = (string)json["Version"];
        Main.Settings.GithubVersion = version;
      } catch (WebException) {
        // Do nothing if there's a problem getting the version from Github
      }
    }

    private static void LoadSettings(string modDirectory) {
      Logger.Log("Loading MissionControl settings");
      JsonSerializerSettings serialiserSettings = new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.All,
        Culture = CultureInfo.InvariantCulture
      };

      string settingsJsonString = File.ReadAllText($"{modDirectory}/settings.json");
      Settings = JsonConvert.DeserializeObject<Config.Settings>(settingsJsonString, serialiserSettings);

      string modJsonString = File.ReadAllText($"{modDirectory}/mod.json");
      JObject json = JObject.Parse(modJsonString);
      string version = (string)json["Version"];
      Settings.Version = version;

      string alPath = $"{modDirectory}/config/AdditionalLances/";
      string additionalLancesJsonString = File.ReadAllText($"{alPath}General.json");
      Settings.AdditionalLances[0] = JsonConvert.DeserializeObject<AdditionalLances>(additionalLancesJsonString, serialiserSettings);

      string difficultyFileName = "Difficulty";
      var filePaths = Directory.GetFiles(alPath).Where(filePath => System.IO.Path.GetFileNameWithoutExtension(filePath).StartsWith(difficultyFileName));

      foreach (string filePath in filePaths) {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        int difficultyLevel = int.Parse(fileName.Replace(difficultyFileName, ""));
        string additionalLanceJsonString = File.ReadAllText(filePath);
        Settings.AdditionalLances[difficultyLevel] = JsonConvert.DeserializeObject<AdditionalLances>(additionalLanceJsonString, serialiserSettings);
      }
    }

    private static void LoadData(string modDirectory) {
      DataManager.Instance.Init(modDirectory);
    }
  }
}