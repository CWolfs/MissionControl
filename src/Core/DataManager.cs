using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MissionControl.Data;

namespace MissionControl {
  public class DataManager {
    private static DataManager instance;
    public static DataManager Instance {
      get {
        if (instance == null) instance = new DataManager();
        return instance;
      }
    }

    public string ModDirectory { get; private set; }
    public Dictionary<string, MLanceOverride> LanceOverrides { get; private set; } = new Dictionary<string, MLanceOverride>();

    private DataManager() {}

    public void Init(string modDirectory) {
      ModDirectory = modDirectory;
      LoadLanceOverries();
    }

    private void LoadLanceOverries() {
      foreach (string file in Directory.GetFiles($"{ModDirectory}/lances", "*.json")) {
        string lanceData = File.ReadAllText(file);
        MLanceOverrideData lanceOverrideData = JsonConvert.DeserializeObject<MLanceOverrideData>(lanceData);
        LanceOverrides.Add(lanceOverrideData.LanceKey, new MLanceOverride(lanceOverrideData));

        if (Main.Settings.DebugMode) {
          Main.Logger.Log($"[DataManager] Loaded lance override '{lanceOverrideData.LanceKey}'");
          Main.Logger.Log($"[DataManager] Lance def id '{lanceOverrideData.LanceDefId}'");
          Main.Logger.Log($"[DataManager] Lance Tag Set items '{string.Join(",", lanceOverrideData.LanceTagSet.Items)}' and source file '{lanceOverrideData.LanceTagSet.TagSetSourceFile}'");
          Main.Logger.Log($"[DataManager] Lance Excluded Tag Set items '{string.Join(",", lanceOverrideData.LanceExcludedTagSet.Items)}' and source file '{lanceOverrideData.LanceExcludedTagSet.TagSetSourceFile}'");
          Main.Logger.Log($"[DataManager] Spawn Effect Tags items '{string.Join(",", lanceOverrideData.LanceTagSet.Items)}' and source file '{lanceOverrideData.LanceTagSet.TagSetSourceFile}'");

          foreach (MUnitSpawnPointOverrideData unitSpawnPointOverrideData in lanceOverrideData.UnitSpawnPointOverride) {
            Main.Logger.Log($"[DataManager] Unit type '{unitSpawnPointOverrideData.UnitType}'");
            Main.Logger.Log($"[DataManager] Unit def id '{unitSpawnPointOverrideData.UnitDefId}'");
            Main.Logger.Log($"[DataManager] Unit Tag Set items '{string.Join(",", unitSpawnPointOverrideData.UnitTagSet.Items)}' and source file '{unitSpawnPointOverrideData.UnitTagSet.TagSetSourceFile}'");
            Main.Logger.Log($"[DataManager] Unit Excluded Tag Set items '{string.Join(",", unitSpawnPointOverrideData.UnitExcludedTagSet.Items)}' and source file '{unitSpawnPointOverrideData.UnitExcludedTagSet.TagSetSourceFile}'");
            Main.Logger.Log($"[DataManager] Spawn Effect Tags items '{string.Join(",", unitSpawnPointOverrideData.SpawnEffectTags.Items)}' and source file '{unitSpawnPointOverrideData.SpawnEffectTags.TagSetSourceFile}'");
            Main.Logger.Log($"[DataManager] Pilot def id '{unitSpawnPointOverrideData.PilotDefId}'");
            Main.Logger.Log($"[DataManager] Pilot Tag Set items '{string.Join(",", unitSpawnPointOverrideData.PilotTagSet.Items)}' and source file '{unitSpawnPointOverrideData.PilotTagSet.TagSetSourceFile}'");
            Main.Logger.Log($"[DataManager] Pilot Excluded Tag Set items '{string.Join(",", unitSpawnPointOverrideData.PilotExcludedTagSet.Items)}' and source file '{unitSpawnPointOverrideData.PilotExcludedTagSet.TagSetSourceFile}'");
          }
        }
      }
    }

    public void Reset() {

    }
  }
}