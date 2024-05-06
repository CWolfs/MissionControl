using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MissionControl.Data {
  public class PropBuildingDef {
    [JsonProperty("Key")]
    public string Key { get; set; }

    [JsonProperty("BuildingDefID")]
    public string BuildingDefID { get; set; }

    [JsonProperty("MainModel")]
    public string MainModelKey { get; set; }

    [JsonProperty("Glass")]
    public PropPositionalDef Glass { get; set; }

    [JsonProperty("Destructibles")]
    public JArray RawDestructibleFlimsyModels { get; set; }

    [JsonProperty("Decals")]
    public JArray RawDecals { get; set; }

    [JsonIgnore]
    public List<PropDestructibleFlimsyDef> DestructibleFlimsyModels { get; set; } = new List<PropDestructibleFlimsyDef>();

    [JsonIgnore]
    public List<PropDecalDef> Decals { get; set; } = new List<PropDecalDef>();

    public PropModelDef GetPropModelDef() {
      if (DataManager.Instance.ModelDefs.ContainsKey(MainModelKey)) {
        return DataManager.Instance.ModelDefs[MainModelKey];
      }

      Main.Logger.LogError($"[{this.GetType().Name}.GetPropModelDef] No PropModelDef found for key '{MainModelKey}'. This should not happen.");
      return null;
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context) {
      if (RawDestructibleFlimsyModels != null) {
        foreach (JObject destructibleFlimsy in RawDestructibleFlimsyModels.Children<JObject>()) {
          string key = destructibleFlimsy["Key"].ToString();
          JObject position = destructibleFlimsy.ContainsKey("Position") ? (JObject)destructibleFlimsy["Position"] : null;
          JObject rotation = destructibleFlimsy.ContainsKey("Rotation") ? (JObject)destructibleFlimsy["Rotation"] : null;
          bool allowModelMeshOffsets = destructibleFlimsy.ContainsKey("AllowModelMeshOffsets") ? (bool)destructibleFlimsy["AllowModelMeshOffsets"] : true;

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

            propDestructibleFlimsyDef.AllowModelMeshOffsets = allowModelMeshOffsets;


            DestructibleFlimsyModels.Add(propDestructibleFlimsyDef);
          } else {
            Main.Logger.LogError($"[{this.GetType().Name}.OnDeserialized] PropDef '{Key}' cannot find PropDestructibleFlimsyDef with key '{key}'. Check your /props/destructibles fodler has a valid definition json for it.");
          }
        }
      }

      if (RawDecals != null) {
        foreach (JObject decal in RawDecals.Children<JObject>()) {
          string key = decal["Key"].ToString();
          JObject position = decal.ContainsKey("Position") ? (JObject)decal["Position"] : null;
          JObject rotation = decal.ContainsKey("Rotation") ? (JObject)decal["Rotation"] : null;
          JObject scale = decal.ContainsKey("Scale") ? (JObject)decal["Scale"] : null;
          int sheetXCoordinate = decal.ContainsKey("SheetXCoordinate") ? (int)decal["SheetXCoordinate"] : 0;
          int sheetYCoordinate = decal.ContainsKey("SheetYCoordinate") ? (int)decal["SheetYCoordinate"] : 0;
          float alpha = decal.ContainsKey("Alpha") ? (float)decal["Alpha"] : 1;
          int priority = decal.ContainsKey("Priority") ? (int)decal["Priority"] : 100;

          if (DataManager.Instance.DecalDefs.ContainsKey(key)) {
            PropDecalDef propDecalDef = DataManager.Instance.DecalDefs[key].Clone();

            if (position != null) {
              Vector3 pos = new Vector3((float)position["x"], (float)position["y"], (float)position["z"]);
              propDecalDef.Position.Value = pos;
            }

            if (rotation != null) {
              Vector3 rot = new Vector3((float)rotation["x"], (float)rotation["y"], (float)rotation["z"]);
              propDecalDef.Rotation.Value = rot;
            }

            if (scale != null) {
              Vector3 scaleVec = new Vector3((float)scale["x"], (float)scale["y"], (float)scale["z"]);
              propDecalDef.Scale.Value = scaleVec;
            }

            propDecalDef.SheetXCoordinate = sheetXCoordinate;
            propDecalDef.SheetYCoordinate = sheetYCoordinate;
            propDecalDef.Alpha = alpha;
            propDecalDef.Priority = priority;

            Decals.Add(propDecalDef);
          } else {
            Main.Logger.LogError($"[{this.GetType().Name}.OnDeserialized] PropDef '{Key}' cannot find PropDecalDef with key '{key}'. Check your /props/decals fodler has a valid definition json for it.");
          }
        }
      }
    }
  }
}