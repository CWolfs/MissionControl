using UnityEngine;
using System;
using System.Collections.Generic;
using HBS.Logging;
using Harmony;
using Newtonsoft.Json;
using System.Reflection;

using SpawnVariation.Utils;

namespace SpawnVariation {
    public class Main {
        public static ILog Logger;
        public static Settings Settings { get; private set; }
        public static Assembly SpawnVariationAssembly { get; set; } 
        public static AssetBundle SpawnVariationBundle { get; set; }
        public static string Path { get; private set; }

        public static void InitLogger(string modDirectory) {
            Dictionary<string, LogLevel> logLevels = new Dictionary<string, LogLevel> {
                ["SpawnVariation"] = LogLevel.Debug
            };
            LogManager.Setup(modDirectory + "/output.log", logLevels);
            Logger = LogManager.GetLogger("SpawnVariation");
            Path = modDirectory;
        }

        // Entry point into the mod, specified in the `mod.json`
        public static void Init(string modDirectory, string modSettings) {
            try {
                InitLogger(modDirectory);

                Logger.Log("Loading SpawnVariation settings");
                Settings = JsonConvert.DeserializeObject<Settings>(modSettings);
            } catch (Exception e) {
                Logger.LogError(e);
                Logger.Log("Error loading mod settings - using defaults.");
                Settings = new Settings();
            }

            HarmonyInstance harmony = HarmonyInstance.Create("co.uk.cwolf.SpawnVariation");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}