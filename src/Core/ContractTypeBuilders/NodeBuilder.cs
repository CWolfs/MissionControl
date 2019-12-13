using UnityEngine;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public abstract class NodeBuilder {
    public void SetPosition(GameObject target, JObject position) {
      string type = position["Type"].ToString();
      JObject value = (JObject)position["Value"];

      if (type == "World") {
        Vector3 worldPosition = new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);
        worldPosition = worldPosition.GetClosestHexLerpedPointOnGrid();
        target.transform.position = worldPosition;
      } else if (type == "Local") {
        Vector3 localPosition = new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);
        localPosition = localPosition.GetClosestHexLerpedPointOnGrid();
        target.transform.position = localPosition;
      } else if (type == "SpawnProfile") {
        // TODO: Allow for spawners to be used
      }
    }

    public void SetRotation(GameObject target, JObject rotation) {
      string type = rotation["Type"].ToString();
      JObject value = (JObject)rotation["Value"];
      Vector3 eulerRotation = new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);

      if (type == "World") {
        target.transform.rotation = Quaternion.Euler(eulerRotation);
      } else if (type == "Local") {
        target.transform.localRotation = Quaternion.Euler(eulerRotation);
      }
    }

    public abstract void Build();
  }
}