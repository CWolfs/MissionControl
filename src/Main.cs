using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using HBS.Logging;
using Harmony;
using Newtonsoft.Json;
using System.Reflection;

using ContractCommand.Utils;

namespace ContractCommand {
    public class Main {
        public static ILog Logger;
        public static Settings Settings { get; private set; }
        public static Assembly ContractCommandAssembly { get; set; } 
        public static AssetBundle ContractCommandBundle { get; set; }
        public static string Path { get; private set; }

        public static void InitLogger(string modDirectory) {
            Dictionary<string, LogLevel> logLevels = new Dictionary<string, LogLevel> {
                ["ContractCommand"] = LogLevel.Debug
            };
            LogManager.Setup(modDirectory + "/output.log", logLevels);
            Logger = LogManager.GetLogger("ContractCommand");
            Path = modDirectory;
        }

        // Entry point into the mod, specified in the `mod.json`
        public static void Init(string modDirectory, string modSettings) {
            try {
                InitLogger(modDirectory);
                LoadSettings(modDirectory);
            } catch (Exception e) {
                Logger.LogError(e);
                Logger.Log("Error loading mod settings - using defaults.");
                Settings = new Settings();
            }

            HarmonyInstance harmony = HarmonyInstance.Create("co.uk.cwolf.ContractCommand");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void LoadSettings(string modDirectory) {
            Logger.Log("Loading ContractCommand settings");
            string settingsJsonString = File.ReadAllText($"{modDirectory}/settings.json");
            Settings = JsonConvert.DeserializeObject<Settings>(settingsJsonString);
        }
    }
}