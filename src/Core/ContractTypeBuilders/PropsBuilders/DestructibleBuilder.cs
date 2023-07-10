using UnityEngine;

using MissionControl.Data;
using MissionControl.EncounterFactories;

using Newtonsoft.Json.Linq;

namespace MissionControl.ContractTypeBuilders {
  public class DestructibleBuilder : NodeBuilder {
    private ContractTypeBuilder contractTypeBuilder;
    private JObject destructibleGroup;

    private string destructibleGroupName;
    private JObject position;
    private JObject rotation;
    private JArray destructibles;
    private JArray buildings;

    public DestructibleBuilder(ContractTypeBuilder contractTypeBuilder, JObject destructibleGroup) {
      this.contractTypeBuilder = contractTypeBuilder;
      this.destructibleGroup = destructibleGroup;

      destructibleGroupName = destructibleGroup["Name"].ToString();
      position = destructibleGroup.ContainsKey("Position") ? (JObject)destructibleGroup["Position"] : null;
      rotation = destructibleGroup.ContainsKey("Rotation") ? (JObject)destructibleGroup["Rotation"] : null;
      destructibles = destructibleGroup.ContainsKey("Destructibles") ? (JArray)destructibleGroup["Destructibles"] : null;
      buildings = destructibleGroup.ContainsKey("Buildings") ? (JArray)destructibleGroup["Buildings"] : null;
    }

    public override void Build() {
      // Build the group parent
      DestructibleFactory destructibleGroupFactory = new DestructibleFactory();
      GameObject destructibleGroupGO = destructibleGroupFactory.CreateDestructibleFlimsyGroup(destructibleGroupName);

      // Build all the flimsy destructibles
      if (destructibles != null) {
        GameObject destructiblesContainerGO = new GameObject("Destructibles");
        destructiblesContainerGO.transform.SetParent(destructibleGroupGO.transform);
        destructiblesContainerGO.transform.position = Vector3.zero;
        destructiblesContainerGO.transform.rotation = Quaternion.identity;

        foreach (JObject destructible in destructibles.Children<JObject>()) {
          string destructibleName = destructible["Name"].ToString();
          JObject position = destructible.ContainsKey("Position") ? (JObject)destructible["Position"] : null;
          JObject rotation = destructible.ContainsKey("Rotation") ? (JObject)destructible["Rotation"] : null;

          if (!DataManager.Instance.DestructibleDefs.ContainsKey(destructibleName)) {
            Main.Logger.LogError($"[DestructibleBuilder.Build] No destructible exists with key '{destructibleName}'. Check a PropDestructionDef exists with that key in the 'props/destructible' folder");
          }

          PropDestructibleFlimsyDef propDestructibleDef = DataManager.Instance.DestructibleDefs[destructibleName];
          DestructibleFactory destructibleFactory = new DestructibleFactory(propDestructibleDef);

          GameObject destructibleGO = destructibleFactory.CreateDestructible(destructiblesContainerGO, propDestructibleDef.Key);

          if (position != null) {
            SetPosition(destructibleGO, position, exactPosition: true);
          }

          if (rotation != null) {
            SetRotation(destructibleGO, rotation);
          }
        }
      }

      // Build all the flimsy buildings
      if (buildings != null) {
        GameObject buildingsContainerGO = new GameObject("Buildings");
        buildingsContainerGO.transform.SetParent(destructibleGroupGO.transform);
        buildingsContainerGO.transform.position = Vector3.zero;
        buildingsContainerGO.transform.rotation = Quaternion.identity;

        foreach (JObject building in buildings.Children<JObject>()) {
          string buildingName = building["Name"].ToString();
          JObject position = building.ContainsKey("Position") ? (JObject)building["Position"] : null;
          JObject rotation = building.ContainsKey("Rotation") ? (JObject)building["Rotation"] : null;

          if (!DataManager.Instance.BuildingDefs.ContainsKey(buildingName)) {
            Main.Logger.LogError($"[DestructibleBuilder.Build] No building exists with key '{buildingName}'. Check a PropBuildingDef exists with that key in the 'props/buildings' folder");
          }

          PropBuildingDef propBuildingDef = DataManager.Instance.BuildingDefs[buildingName];
          BuildingFactory buildingFactory = new BuildingFactory(propBuildingDef);

          GameObject destructibleGO = buildingFactory.CreateBuilding(buildingsContainerGO, $"Building_{propBuildingDef.Key}", DestructibleObject.DestructType.flimsyStruct);

          if (position != null) {
            SetPosition(destructibleGO, position, exactPosition: true);
          }

          if (rotation != null) {
            SetRotation(destructibleGO, rotation);
          }
        }
      }

      DestructibleFlimsyGroup destructibleFlimsyGroup = destructibleGroupGO.GetComponent<DestructibleFlimsyGroup>();
      if (destructibleFlimsyGroup != null) destructibleFlimsyGroup.BakeDestructionAssets();

      if (this.position != null) {
        SetPosition(destructibleGroupGO, this.position, exactPosition: true);
      }

      if (this.rotation != null) {
        SetRotation(destructibleGroupGO, this.rotation);
      }
    }
  }
}