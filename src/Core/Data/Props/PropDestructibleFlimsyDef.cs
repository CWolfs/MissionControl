using UnityEngine;

using Newtonsoft.Json;

namespace MissionControl.Data {
  public class PropDestructibleFlimsyDef {
    [JsonProperty("Key")]
    public string Key { get; set; } // Key in 'props/destructibles' folder (if a PropDestructibleDef), or 'props/models' (if a building flimsy)

    [JsonProperty("MainModel")]
    public string ModelKey { get; set; } // Key in 'props/models' folder only

    [JsonProperty("Position")]
    public Vector3 Position { get; set; } = Vector3.zero;

    [JsonProperty("Rotation")]
    public Vector3 Rotation { get; set; } = Vector3.zero;

    [JsonProperty("Mass")]
    public float Mass { get; set; } = 1000;

    public PropModelDef GetPropModelDef() {
      if (DataManager.Instance.ModelDefs.ContainsKey(Key)) {
        return DataManager.Instance.ModelDefs[Key];
      }

      if (DataManager.Instance.ModelDefs.ContainsKey(ModelKey)) {
        return DataManager.Instance.ModelDefs[ModelKey];
      }

      Main.Logger.LogError($"[PropDestructibleFlimsyDef.GetPropModelDef] No PropModelDef found for Flimsy with key '{Key}' or '{ModelKey}'. This should not happen.");
      return null;
    }

    public PropDestructibleFlimsyDef Clone() {
      PropDestructibleFlimsyDef newPropDestructibleFlimsyDef = new PropDestructibleFlimsyDef();
      newPropDestructibleFlimsyDef.Key = Key;
      newPropDestructibleFlimsyDef.ModelKey = ModelKey;
      newPropDestructibleFlimsyDef.Position = Position;
      newPropDestructibleFlimsyDef.Rotation = Rotation;
      newPropDestructibleFlimsyDef.Mass = Mass;
      return newPropDestructibleFlimsyDef;
    }
  }
}