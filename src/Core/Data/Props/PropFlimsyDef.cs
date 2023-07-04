using UnityEngine;

using Newtonsoft.Json;

namespace MissionControl.Data {
  public class PropFlimsyDef {
    [JsonProperty("Key")]
    public string Key { get; set; }

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

      Main.Logger.LogError($"[PropFlimsyDef.GetPropModelDef] No PropModelDef found for Flimsy with key '{Key}'. This should not happen.");
      return null;
    }
  }
}