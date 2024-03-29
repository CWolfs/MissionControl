using UnityEngine;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MissionControl.Data {
  public class VectorData {
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VectorType { World, Local }

    [JsonProperty("Type", Order = 0)]
    public VectorType Type = VectorType.World;

    [JsonProperty("Value", Order = 1)]
    public Vector3 Value;

    public VectorData() {
      Value = Vector3.zero;
    }

    public VectorData(Vector3 value) {
      Value = value;
    }

    public VectorData(VectorType type) {
      Value = Vector3.zero;
      Type = type;
    }

    public VectorData(VectorType type, float x, float y, float z) {
      Type = type;
      Value = new Vector3(x, y, z);
    }

    public VectorData(float x, float y, float z) {
      Value = new Vector3(x, y, z);
    }

    public VectorData(VectorType type, Vector3 value) {
      this.Type = type;
      this.Value = value;
    }

    public VectorData Clone() {
      return new VectorData(Type, Value.x, Value.y, Value.z);
    }

    public override string ToString() {
      return $"Type: {Type}, Value: {Value} | ({Value.x}, {Value.y}, {Value.z})";
    }
  }
}