using UnityEngine;

using Newtonsoft.Json.Linq;

using BattleTech;

namespace MissionControl.ContractTypeBuilders {
  public abstract class NodeBuilder {
    public void SetPosition(GameObject target, JObject position, bool preciseSpawnPoints = false) {
      string type = position.ContainsKey("Type") ? position["Type"].ToString() : "Local";
      JObject value = position.ContainsKey("Value") ? (JObject)position["Value"] : position;

      if (type == "World") {
        Vector3 worldPosition = new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);
        if (preciseSpawnPoints) {
          worldPosition.y = UnityGameInstance.BattleTechGame.Combat.MapMetaData.GetLerpedHeightAt(worldPosition);
        } else {
          worldPosition = worldPosition.GetClosestHexLerpedPointOnGrid();
        }
        target.transform.position = worldPosition;
      } else if (type == "Local") {
        Vector3 localPosition = new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);
        target.transform.localPosition = localPosition;
        Vector3 worldPosition = target.transform.position;
        if (preciseSpawnPoints) {
          worldPosition.y = UnityGameInstance.BattleTechGame.Combat.MapMetaData.GetLerpedHeightAt(worldPosition);
        } else {
          worldPosition = worldPosition.GetClosestHexLerpedPointOnGrid();
        }
        worldPosition.y += localPosition.y;
        target.transform.position = worldPosition;
      } else if (type == "SpawnProfile") {
        // TODO: Allow for spawners to be used
      }
    }

    public void SetRotation(GameObject target, JObject rotation) {
      string type = rotation.ContainsKey("Type") ? rotation["Type"].ToString() : "Local";
      JObject value = rotation.ContainsKey("Value") ? (JObject)rotation["Value"] : rotation;
      Vector3 eulerRotation = new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);

      if (type == "World") {
        target.transform.rotation = Quaternion.Euler(eulerRotation);
      } else if (type == "Local") {
        target.transform.localRotation = Quaternion.Euler(eulerRotation);
      }
    }

    public void SetMountOnPosition(GameObject target, string mountTargetPath) {
      MissionControl.Instance.QueuedBuildingMounts.Add(new object[] { target, mountTargetPath });
    }

    public abstract void Build();
  }
}