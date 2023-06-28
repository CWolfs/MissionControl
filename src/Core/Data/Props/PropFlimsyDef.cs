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
  }
}