using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MissionControl.Data {
  public class PropStructureDef {
    [JsonProperty("Key")]
    public string Key { get; set; }

    [JsonProperty("MainModel")]
    public string MainModelKey { get; set; }

    [JsonProperty("Destructibles")]
    public JArray RawDestructibleFlimsyModels { get; set; }

    public List<PropDestructibleFlimsyDef> DestructibleFlimsyModels { get; set; } = new List<PropDestructibleFlimsyDef>();

    public PropModelDef GetPropModelDef() {
      if (DataManager.Instance.ModelDefs.ContainsKey(MainModelKey)) {
        return DataManager.Instance.ModelDefs[MainModelKey];
      }

      Main.Logger.LogError($"[PropStructureDef.GetPropModelDef] No PropModelDef found for key '{MainModelKey}'. This should not happen.");
      return null;
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context) {
      if (RawDestructibleFlimsyModels == null) return;

      foreach (JObject destructibleFlimsy in RawDestructibleFlimsyModels.Children<JObject>()) {
        string key = destructibleFlimsy["Key"].ToString();
        JObject position = destructibleFlimsy.ContainsKey("Position") ? (JObject)destructibleFlimsy["Position"] : null;
        JObject rotation = destructibleFlimsy.ContainsKey("Rotation") ? (JObject)destructibleFlimsy["Rotation"] : null;

        if (DataManager.Instance.DestructibleDefs.ContainsKey(key)) {
          PropDestructibleFlimsyDef propDestructibleFlimsyDef = DataManager.Instance.DestructibleDefs[key].Clone();

          // Override default position and rotation for buildings if they are provided
          if (position != null) {
            Vector3 pos = new Vector3((float)position["x"], (float)position["y"], (float)position["z"]);
            propDestructibleFlimsyDef.Position = pos;
          }

          if (rotation != null) {

            Vector3 rot = new Vector3((float)rotation["x"], (float)rotation["y"], (float)rotation["z"]);
            propDestructibleFlimsyDef.Rotation = rot;
          }

          DestructibleFlimsyModels.Add(propDestructibleFlimsyDef);
        } else {
          Main.Logger.LogError($"[PropStructureDef.OnDeserialized] PropStructureDef '{Key}' cannot find PropDestructibleFlimsyDef with key '{key}'. Check your /props/destructibles fodler has a valid definition json for it.");
        }
      }
    }
  }
}