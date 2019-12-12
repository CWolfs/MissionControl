using UnityEngine;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public abstract class NodeBuilder {
    public void SetPosition(GameObject target, JObject position) {
      string type = position["Type"].ToString();
      JObject value = (JObject)position["Value"];

      if (type == "World") {
        Vector3 worldPosition = new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);
        target.transform.position = worldPosition;
      } else if (type == "Local") {
        Vector3 localPosition = new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);
        target.transform.position = localPosition;
      } else if (type == "SpawnProfile") {
        // TODO: Allow for spawners to be used
      }
    }

    public abstract void Build();
  }
}